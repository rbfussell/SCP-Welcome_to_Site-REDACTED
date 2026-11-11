#!/usr/bin/env python3
"""Evidence-backed software slices; no spawning, fake workers, or model dependency."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
STATES = {'proposed', 'ready', 'active', 'blocked', 'done'}
REQUIRED_CHECKS = ('tests', 'docs', 'wiki-build', 'wiki-check')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def stamp():
    return datetime.now(timezone.utc).isoformat()


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


class SoftwareProject:
    def __init__(self, root=ROOT):
        self.root = Path(root).resolve()

    def path(self, value):
        path = (self.root / value).resolve()
        require(path.is_relative_to(self.root), f'Path escapes game repository: {value}')
        return path

    def read(self, value):
        return json.loads(self.path(value).read_text())

    def task_path(self, task_id):
        require(re.fullmatch(r'[a-z0-9]+(?:-[a-z0-9]+)*', task_id), 'Invalid task ID')
        return self.path(f'management/tasks/{task_id}.json')

    def task(self, task_id):
        value = json.loads(self.task_path(task_id).read_text())
        require(value['id'] == task_id, 'Task ID does not match filename')
        return value

    def save(self, value, action):
        value.setdefault('history', []).append({'at': stamp(), 'action': action})
        self.task_path(value['id']).write_text(json.dumps(value, indent=2) + '\n')
        return value

    def profile(self):
        profile = self.read('management/profile.json')
        require(profile['profile'] == 'software-game' and profile['execution'] == 'single-developer', 'Wrong project profile')
        return profile

    def tasks(self):
        return [self.task(p.stem) for p in sorted(self.path('management/tasks').glob('*.json'))]

    def status(self):
        return {'profile': self.profile()['profile'], 'game_root': str(self.root),
                'tasks': [{'id': t['id'], 'title': t['title'], 'status': t['status']} for t in self.tasks()]}

    def snapshot(self, task):
        # Declared inputs only, not a claim to detect every possible repository change.
        return {name: sha(self.path(name)) for name in task['inputs']}

    def context(self, value):
        require(value.get('wiki_read') and value.get('docs_read'), 'Read wiki AND project documents first')
        mandatory = {'source_material/game-wiki/content/status.md', 'source_material/game-wiki/content/development.md'}
        require(mandatory <= {x['path'] for x in value['wiki_read']}, 'Read implementation ledger and development workflow')
        receipts = []
        for group in ('wiki_read', 'docs_read'):
            for entry in value[group]:
                require(len(entry.get('notes', '').strip()) >= 20, 'Context needs substantive reading notes')
                path = self.path(entry['path'])
                require(path.is_file(), f'Missing context: {entry["path"]}')
                if group == 'wiki_read':
                    require(path.is_relative_to(self.path('source_material/game-wiki')), 'Canon dump is not game-wiki context')
                receipts.append({**entry, 'sha256': sha(path), 'kind': group})
        return {'recorded_at': stamp(), 'attestation': 'Developer reports reading; hashes do not prove reading.', 'sources': receipts}

    def start(self, task_id, context):
        task = self.task(task_id)
        require(task['status'] in {'ready', 'blocked'}, 'Only ready or blocked slices can start')
        require(not any(t['status'] == 'active' and t['id'] != task_id for t in self.tasks()), 'Finish or block the active slice first')
        require(not task.get('blocking_questions'), 'Resolve blocking design questions before dependent work')
        require(all(self.task(t)['status'] == 'done' for t in task.get('depends_on', [])), 'Dependencies are incomplete')
        task.update(status='active', context=self.context(context), checks={}, review=None)
        return self.save(task, 'started with wiki/document context')

    def record(self, task_id, label, command):
        task = self.task(task_id)
        require(task['status'] == 'active', 'Checks belong to an active slice')
        require(re.fullmatch(r'[a-z0-9-]+', label) and command, 'Provide a check label and actual argv command')
        before = self.snapshot(task)
        folder = self.path(f'management/evidence/{task_id}')
        folder.mkdir(parents=True, exist_ok=True)
        log = folder / f'{label}-{datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")}.log'
        with log.open('x') as output:
            output.write(json.dumps({'command': command, 'cwd': 'game repository', 'started': stamp()}) + '\n')
            output.flush()
            result = subprocess.run(command, cwd=self.root, stdout=output, stderr=subprocess.STDOUT, check=False)
        after = self.snapshot(task)
        task.setdefault('checks', {})[label] = {'command': command, 'exit_code': result.returncode,
            'result': 'PASS' if result.returncode == 0 else 'FAIL', 'finished_at': stamp(),
            'log': log.relative_to(self.root).as_posix(), 'sha256': sha(log),
            'inputs': after, 'inputs_changed_during_check': before != after}
        self.save(task, f'{label}: {task["checks"][label]["result"]}')
        return task['checks'][label]

    def verify_checks(self, task, current=False):
        checks = task.get('checks', {})
        for label in REQUIRED_CHECKS:
            require(label in checks and checks[label]['result'] == 'PASS' and checks[label]['exit_code'] == 0,
                    f'{task["id"]}: missing passing {label} check')
            entry = checks[label]
            require(sha(self.path(entry['log'])) == entry['sha256'], f'Changed check log: {label}')
            require(not entry['inputs_changed_during_check'], f'Inputs changed during {label}; rerun')
            if current:
                require(entry['inputs'] == self.snapshot(task), f'Stale {label} evidence; inputs changed')
        require(checks['wiki-check']['finished_at'] >= checks['wiki-build']['finished_at'], 'Wiki check must follow rebuild')

    def complete(self, task_id, review):
        task = self.task(task_id)
        require(task['status'] == 'active' and not task.get('blocking_questions'), 'Slice inactive or blocked by design questions')
        self.verify_checks(task, current=True)
        require(review.get('mode') in {'self-review', 'independent'}, 'Declare actual review mode')
        require(len(review.get('summary', '').strip()) >= 30 and review.get('implementation_docs_agree') is True, 'Record semantic consistency review')
        require(isinstance(review.get('limitations'), list), 'Record remaining limitations')
        require(review.get('acceptance') and all(x.get('result') == 'PASS' and x.get('evidence') for x in review['acceptance']), 'Acceptance needs evidence')
        require({x['criterion'] for x in review['acceptance']} == set(task['acceptance']), 'Review every acceptance criterion')
        if review['mode'] == 'independent':
            require(review.get('reviewer_receipt'), 'Independent review needs an actual worker/human receipt')
        else:
            require(not review.get('reviewer_receipt'), 'Self-review cannot claim an independent worker')
        task.update(status='done', review=review, accepted_inputs=self.snapshot(task))
        return self.save(task, 'completed; evidence and semantic review recorded')

    def check(self):
        self.profile()
        tasks = self.tasks()
        require(sum(t['status'] == 'active' for t in tasks) <= 1, 'Multiple active slices')
        for task in tasks:
            require(task['status'] in STATES and task['title'].strip(), 'Invalid task state/title')
            require(task['acceptance'] and task['inputs'] and task['wiki_pages'], 'Task needs acceptance, inputs and affected wiki pages')
            self.snapshot(task)
            for path in task['wiki_pages']:
                require(self.path(path).is_file(), f'Missing affected wiki page: {path}')
            for dependency in task.get('depends_on', []):
                require(dependency != task['id'], 'Task cannot depend on itself')
                self.task(dependency)
            if task['status'] == 'done':
                self.verify_checks(task)  # Accepted snapshots remain historical after later development.
                require(task.get('review') and task.get('accepted_inputs'), 'Done slice lacks review/snapshot')
        return {'result': 'PASS', 'tasks': len(tasks), 'note': 'Record integrity; not gameplay qualification or independent audit.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    subs = parser.add_subparsers(dest='action', required=True)
    for name in ('status', 'check'): subs.add_parser(name)
    start = subs.add_parser('start'); start.add_argument('id'); start.add_argument('--context', required=True)
    record = subs.add_parser('record'); record.add_argument('id'); record.add_argument('label'); record.add_argument('command', nargs=argparse.REMAINDER)
    finish = subs.add_parser('complete'); finish.add_argument('id'); finish.add_argument('--review', required=True)
    block = subs.add_parser('block'); block.add_argument('id'); block.add_argument('--reason', required=True)
    args = parser.parse_args(); project = SoftwareProject(args.root)
    if args.action == 'status': result = project.status()
    elif args.action == 'check': result = project.check()
    elif args.action == 'start': result = project.start(args.id, project.read(args.context))
    elif args.action == 'record':
        command = args.command[1:] if args.command[:1] == ['--'] else args.command
        result = project.record(args.id, args.label, command)
    elif args.action == 'complete': result = project.complete(args.id, project.read(args.review))
    else:
        task = project.task(args.id)
        require(task['status'] in {'active', 'ready'} and len(args.reason.strip()) >= 15, 'Record a specific active/ready-work blocker')
        task.update(status='blocked', blocker=args.reason)
        result = project.save(task, args.reason)
    print(json.dumps(result, indent=2))
    return 1 if result.get('result') == 'FAIL' else 0


if __name__ == '__main__':
    try: sys.exit(main())
    except (ValueError, KeyError, OSError) as error:
        print(f'BLOCKED: {error}', file=sys.stderr); sys.exit(1)
