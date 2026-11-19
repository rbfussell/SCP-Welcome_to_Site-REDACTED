"""Preflight progression, independent questions, research queues and prototype checks."""
import hashlib
from urllib.parse import urlparse

TERMINAL = {'EVC-28', 'IA-11', 'DOC-10'}


class Preflight:
    def history(self):
        """Include interrupted evidence. Only IA-accepted commits can resolve a blocker."""
        records = []
        for p in sorted(self.path('evidence').glob('turn.*.json')):
            turn = self.turn(p.stem)
            wiki = self.path(f'wiki/{turn["turn_id"]}.json')
            doc = self.read(wiki) if wiki.exists() else None
            records.append({'turn': turn, 'doc': doc,
                            'accepted': bool(doc and doc['audit']['disposition'] == 'ACCEPTED')})
        return records

    def preflight_status(self):
        questions, requests = {}, {}
        setup = surveyed = researched = baseline = False
        for row in self.history():
            turn, doc, accepted = row['turn'], row['doc'], row['accepted']
            if accepted:
                setup |= bool(doc.get('setup_ready'))
                surveyed |= turn['assessment']['kind'] == 'questions'
                researched |= turn['assessment']['kind'] == 'research'
                baseline |= any(e['entity_id'] == 'architecture.baseline' for e in doc['entities'])
            for event in turn['events']:
                kind, data = event['kind'], event['data']
                if kind == 'HANDOFF':
                    for question in data.get('question_review', {}).get('questions', []):
                        questions.setdefault(question['question_id'], {**question, 'agent_id': data['agent_id'],
                                                                      'turn_id': turn['turn_id'], 'answer': None})
                elif kind == 'QUESTION':
                    questions.setdefault(data['question_id'], {**data, 'answer': None})
                elif kind == 'ANSWER' and accepted and data['question_id'] in questions:
                    questions[data['question_id']]['answer'] = data
                elif kind == 'RESEARCH_REQUEST':
                    requests[data['request_id']] = {**data, 'turn_id': turn['turn_id'], 'resolution': None}
                elif kind == 'RESEARCH_RESPONSE' and accepted and data['request_id'] in requests:
                    requests[data['request_id']]['resolution'] = data
        open_questions = [q for q in questions.values() if q['answer'] is None]
        pending = [r for r in requests.values() if r['resolution'] is None]
        findings = self.outstanding()
        if not setup: next_kind = 'setup'
        elif findings: next_kind = 'reconcile'
        elif not surveyed: next_kind = 'questions'
        elif not researched or pending: next_kind = 'research'
        elif open_questions or not baseline: next_kind = 'reconcile'
        else: next_kind = 'work'
        return {'setup_complete': setup, 'question_round_complete': surveyed,
                'initial_research_complete': researched, 'baseline_complete': baseline,
                'questions': list(questions.values()), 'unanswered_questions': open_questions,
                'blocking_questions': [q for q in open_questions if q['blocking']],
                'research_requests': list(requests.values()), 'pending_research': pending,
                'open_ia_cases': findings, 'next_kind': next_kind,
                'production_allowed': next_kind == 'work'}

    def initial_prerequisites_ready(self):
        """Hook for a reviewed existing-project import without inventing research history."""
        return self.preflight_status()['initial_research_complete']

    def _prepare_preflight(self, assessment):
        from workflow import require
        kind = assessment['kind']
        status = self.preflight_status()
        expected = status['next_kind']
        require(kind in ('setup', 'questions', 'research', 'reconcile', 'work'), 'Invalid preflight/work turn kind')
        if not status['setup_complete']:
            require(kind == 'setup', 'The first turn must be setup; failed setup repeats before later stages')
        elif kind == 'work':
            require(status['production_allowed'], f'Production is blocked; next preflight turn is {expected}')
        else:
            require(kind == expected or (kind == 'reconcile' and bool(status['open_ia_cases'])),
                    f'Preflight order requires {expected}, not {kind}')
        conditional = []
        if kind in ('setup', 'questions', 'research', 'reconcile') or assessment['architecture_change']:
            conditional.append('KRA-14')
        if kind == 'questions':
            conditional.append('RES-09')
        if kind == 'research' or status['pending_research']:
            conditional.append('RES-09')
            assessment['research_required'] = True
        # Requests raised in work run next turn, never inline in the requesting turn.
        if assessment['research_required'] and kind not in ('setup', 'questions'):
            conditional.append('RES-09')
        if kind in ('setup', 'questions'):
            require(not assessment['research_required'], 'Open-web product research starts on the research turn')
        if kind == 'work' and assessment.get('milestone'):
            require(isinstance(assessment['milestone'], str), 'Milestone must have a stable identifier')
            conditional += ['PRT-05', 'BLD-27']
        return sorted(set(conditional)), status

    def _validate_question_review(self, turn, handoff):
        from workflow import require, substantive
        aid = handoff['agent_id']
        if aid in TERMINAL:
            require('question_review' not in handoff, 'Terminal roles raise questions through findings and owner follow-up, not an execution survey')
            return
        if turn['assessment']['kind'] != 'questions' and 'question_review' not in handoff:
            return
        review = handoff.get('question_review', {})
        require(substantive(review.get('coverage')), 'Each selected agent must independently review ambiguity in its own scope')
        questions = review.get('questions')
        require(isinstance(questions, list), 'Question list required, including an explicit empty list when none are found')
        known = {q['question_id'] for q in self.preflight_status()['questions']}
        for q in questions:
            self._validate_question(q)
            require(q['question_id'] not in known, 'Duplicate question identifier')
            known.add(q['question_id'])
        others = {v['receipt'] for a, v in turn['invocations'].items() if a != aid and a not in TERMINAL}
        if turn['assessment']['kind'] == 'questions':
            require(turn['invocations'][aid]['receipt'] not in others,
                    'Question reviews require distinct independent agent invocation receipts')

    def _validate_question(self, q):
        from workflow import require, substantive
        require(substantive(q.get('question_id'), 3) and substantive(q.get('text')) and substantive(q.get('scope'), 3),
                'Question identifier, text and scope required')
        require(type(q.get('blocking')) is bool, 'Question blocking status must be explicit')

    def ask_question(self, turn_id, agent_id, question):
        from workflow import require
        with self.locked():
            turn = self.turn(turn_id)
            self._active_specialist(turn, agent_id)
            self._validate_question(question)
            require(question['question_id'] not in {q['question_id'] for q in self.preflight_status()['questions']}, 'Duplicate question')
            self._event(turn_id, 'QUESTION', {**question, 'agent_id': agent_id, 'turn_id': turn_id})
        return question

    def answer_question(self, turn_id, question_id, answer, evidence):
        from workflow import require, substantive
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] == 'RESEARCH_AND_EXECUTION', 'User answers must be recorded before evidence collection')
            status = self.preflight_status()
            question = next((q for q in status['questions'] if q['question_id'] == question_id), None)
            require(question is not None and question['answer'] is None, 'Unknown or already answered question')
            require(substantive(answer), 'Record the actual user answer; do not silently select a default')
            refs = self.references(evidence)
            require(refs, 'User answer needs the host message/transcript evidence, without secrets')
            prior = [e for e in turn['events'] if e['kind'] == 'ANSWER' and e['data']['question_id'] == question_id]
            require(not prior, 'Answer is already recorded this turn')
            data = {'question_id': question_id, 'answer': answer, 'actor': 'user', 'evidence': refs}
            self._event(turn_id, 'ANSWER', data)
        return data

    def _active_specialist(self, turn, agent_id):
        from workflow import require
        require(turn['state'] == 'RESEARCH_AND_EXECUTION', 'Specialist work is frozen outside execution')
        require(agent_id in turn['invocations'] and agent_id not in turn['handoffs'] and agent_id not in turn['failures'], 'No active invocation')
        require(agent_id not in TERMINAL, 'Terminal roles cannot perform production or request development research')

    def request_research(self, turn_id, agent_id, request):
        from workflow import require, substantive
        with self.locked():
            turn = self.turn(turn_id)
            self._active_specialist(turn, agent_id)
            require(agent_id != 'RES-09', 'Researcher reports an unresolved research limitation instead of self-queuing')
            require(turn['assessment']['kind'] in ('work','reconcile') and self.initial_prerequisites_ready(), 'New development research requests follow the initial three-stage preflight')
            require(substantive(request.get('request_id'), 3) and substantive(request.get('topic'))
                    and substantive(request.get('reason')), 'Research request needs an ID, topic and reason')
            require(isinstance(request.get('options_needed'), list) and request['options_needed'], 'List the materials, alternatives or information needed')
            known = {r['request_id'] for r in self.preflight_status()['research_requests']}
            require(request['request_id'] not in known, 'Duplicate research request')
            data = {**request, 'agent_id': agent_id, 'evidence': self.references(request.get('evidence', [])),
                    'requested_turn': turn_id, 'production_status': 'WAITING_FOR_RESEARCH'}
            self._event(turn_id, 'RESEARCH_REQUEST', data)
        return data

    def waiting_agents(self, turn):
        return {e['data']['agent_id'] for e in turn['events'] if e['kind'] == 'RESEARCH_REQUEST'}

    def respond_research(self, turn_id, request_id, findings, evidence):
        from workflow import require, substantive
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] == 'RESEARCH_AND_EXECUTION' and 'RES-09' in turn['handoffs'], 'Researcher handoff required before response')
            require(turn['handoffs']['RES-09']['handoff_status'] == 'COMPLETE', 'Research is incomplete')
            request = next((r for r in self.preflight_status()['pending_research'] if r['request_id'] == request_id), None)
            require(request is not None and request['requested_turn'] != turn_id, 'Research requests are serviced on a following turn')
            require(not any(e['kind'] == 'RESEARCH_RESPONSE' and e['data']['request_id'] == request_id for e in turn['events']), 'Response already recorded')
            require(substantive(findings), 'Research response findings required')
            refs = self.references(evidence)
            require(refs, 'Research response needs source evidence')
            data = {'request_id': request_id, 'findings': findings, 'evidence': refs, 'agent_id': 'RES-09'}
            self._event(turn_id, 'RESEARCH_RESPONSE', data)
        return data

    def _validate_research(self, turn, handoff):
        from workflow import require, substantive
        if turn['assessment']['kind'] in ('setup','questions') or handoff['agent_id'] != 'RES-09' or handoff['handoff_status'] != 'COMPLETE':
            return
        research = handoff.get('research', {})
        require(research.get('mode') == 'OPEN_WEB', 'Product/material/options research must use open-web sources')
        require(substantive(research.get('summary')), 'Research findings summary required')
        require(isinstance(research.get('sources'), list) and research['sources'], 'Research needs retrievable source provenance')
        for source in research['sources']:
            url = urlparse(source.get('url', ''))
            require(url.scheme in ('http', 'https') and bool(url.netloc), 'Research source URL required')
            require(substantive(source.get('applicability')), 'Explain source applicability and limitations')
            source['evidence'] = self.references(source.get('evidence', []))
            require(source['evidence'], 'Save a source retrieval/tool receipt; a URL alone is not browsing evidence')

    def _validate_architecture(self, turn, handoff):
        from workflow import require
        if handoff['agent_id'] != 'KRA-14':
            return
        architecture = handoff.get('architecture', {})
        require(isinstance(architecture.get('unresolved_questions'), list), 'Krampus must explicitly identify unresolved architecture questions')
        known = {q['question_id'] for q in self.preflight_status()['questions']}
        known |= {q['question_id'] for q in handoff.get('question_review',{}).get('questions',[])}
        require(all(qid in known for qid in architecture['unresolved_questions']), 'Register scoped questions before referring to unresolved architecture question IDs')
        candidates = architecture.get('baseline_artifacts', [])
        if candidates:
            require(not architecture['unresolved_questions'], 'Unresolved critical architecture questions block baseline completion')
            status = self.preflight_status()
            current_answers = {e['data']['question_id'] for e in turn['events'] if e['kind'] == 'ANSWER'}
            require(not [q for q in status['unanswered_questions'] if q['question_id'] not in current_answers], 'Resolve all outstanding user questions before drafting final baselines')
            require(len(candidates) == 2 and {self.path(p).name for p in candidates} == {'README.md', 'product_technical_description.md'}, 'Both baseline candidate filenames required')
            architecture['baseline_artifacts'] = [r['path'] for r in self.references(candidates)]
        # Setup and ambiguity turns cannot freeze a product baseline before research.
        require(not candidates or turn['assessment']['kind'] in ('research', 'reconcile', 'work'), 'Baseline drafting follows the questions stage')

    def _validate_prototype(self, turn, handoff):
        from workflow import require, substantive, digest
        milestone = turn['assessment'].get('milestone')
        aid = handoff['agent_id']
        if not milestone or aid not in ('PRT-05', 'BLD-27') or handoff['handoff_status'] != 'COMPLETE':
            return
        if aid == 'PRT-05':
            plan = handoff.get('prototype_plan', {})
            require(plan.get('milestone_id') == milestone, 'Prototype plan must identify this milestone')
            require(type(plan.get('feasible')) is bool and substantive(plan.get('best_feasible_subassembly')), 'Challenge full build feasibility and identify the best feasible subset')
            require(isinstance(plan.get('construction_steps'), list) and plan['construction_steps'], 'Concrete build steps or feasibility-check steps required')
            require(isinstance(plan.get('test_criteria'), list) and plan['test_criteria'], 'Prototype acceptance checks required')
            plan['available_artifacts'] = self.references(plan.get('available_artifacts', []))
            if not plan['feasible']:
                require(bool(plan.get('blockers')), 'An infeasibility claim needs identified blockers')
        else:
            designer = turn['handoffs'].get('PRT-05', {})
            require(designer.get('handoff_status') == 'COMPLETE' and 'prototype_plan' in designer, 'Prototype Designer must deliver the current plan before Builder')
            attempt = handoff.get('prototype_attempt', {})
            require(attempt.get('milestone_id') == milestone and attempt.get('plan_hash') == digest(designer['prototype_plan']), 'Build attempt must use this milestone and exact design plan')
            require(type(attempt.get('attempted')) is bool and attempt.get('outcome') in ('BUILT', 'PARTIAL', 'FAILED', 'NOT_FEASIBLE', 'BLOCKED'), 'Actual build-attempt outcome required')
            attempt['test_evidence'] = self.references(attempt.get('test_evidence', []))
            require(attempt['test_evidence'], 'Prove the build attempt or concrete inability with evidence')
            if not attempt['attempted']:
                require(attempt['outcome'] in ('NOT_FEASIBLE', 'BLOCKED'), 'Cannot claim built, partial or failed without attempting construction')
                require(bool(attempt.get('blockers')), 'No-build outcome needs a proven blocker, including missing authorization when applicable')
            for blocker in attempt.get('blockers', []):
                require(substantive(blocker.get('reason')), 'Prototype blocker reason required')
                blocker['evidence'] = self.references(blocker.get('evidence', []))
                require(blocker['evidence'], 'A prototype blocker must have evidence')

    def evidence_inventory(self, turn):
        """Index every hashed reference, including nested source receipts and user answers."""
        refs = {}
        def visit(value):
            if isinstance(value,dict):
                if 'path' in value and 'sha256' in value:
                    prior = refs.get(value['path'])
                    from workflow import require
                    require(prior is None or prior['sha256'] == value['sha256'], 'Conflicting recorded hashes for the same artifact path')
                    refs[value['path']] = value
                else:
                    for child in value.values(): visit(child)
            elif isinstance(value,list):
                for child in value: visit(child)
        for event in turn['events']:
            if event['kind'] in ('HANDOFF','ANSWER','RESEARCH_REQUEST','RESEARCH_RESPONSE','TOOL_RESULT'):
                visit(event['data'])
        return [refs[k] for k in sorted(refs)]

    def build_evidence_package(self, turn_id):
        from workflow import require, digest
        with self.locked():
            turn = self.turn(turn_id)
            require(turn['state'] == 'HANDOFF_COLLECTION' and 'EVC-28' in turn['invocations'], 'Evidence Collector executes after specialists and before IA')
            require('EVC-28' not in turn['handoffs'], 'Evidence collection already finalized')
            expected = sorted(a for a, s in turn['selection'].items() if s['selected'] and a not in TERMINAL)
            inventory = self.evidence_inventory(turn)
            integrity = []
            for ref in inventory:
                try: self.references([ref])
                except (ValueError, FileNotFoundError): integrity.append(ref['path'])
            package = {'turn_id': turn_id, 'collected_head_hash': turn['events'][-1]['hash'],
                       'reviewed_agents': expected, 'missing_invocations': [a for a in expected if a not in turn['invocations']],
                       'missing_handoffs': [a for a in expected if a not in turn['handoffs']],
                       'failed_invocations': turn['failures'], 'artifact_inventory': inventory,
                       'integrity_issues': integrity, 'selection_hash': digest(turn['selection'])}
            path = f'{turn["invocations"]["EVC-28"]["owned_directory"]}/evidence-package.json'
            self.create(path, package)
        return {'path': path, 'package': package}

    def _validate_collection(self, turn, handoff):
        from workflow import require, digest
        if handoff['agent_id'] != 'EVC-28': return
        require(handoff['handoff_status'] == 'COMPLETE', 'Collector must finish its completeness report, even when it identifies missing work')
        path = handoff.get('collection', {}).get('package_path', '')
        owned = self.path(turn['invocations']['EVC-28']['owned_directory'])
        require(path and self.path(path).is_relative_to(owned), 'Collector package must belong to Evidence Collector')
        package = self.read(path)
        expected = sorted(a for a,s in turn['selection'].items() if s['selected'] and a not in TERMINAL)
        require(package['turn_id'] == turn['turn_id'] and package['reviewed_agents'] == expected, 'Collector must account for all selected execution roles')
        require(package['missing_invocations'] == [a for a in expected if a not in turn['invocations']], 'Collector omitted a missing invocation')
        require(package['missing_handoffs'] == [a for a in expected if a not in turn['handoffs']], 'Collector omitted a missing handoff')
        require(package['artifact_inventory'] == self.evidence_inventory(turn), 'Collector omitted or changed indexed evidence')
        require(package['collected_head_hash'] == turn['events'][-1]['hash'] and package['selection_hash'] == digest(turn['selection']), 'Collector reviewed a stale evidence snapshot')
        require(any(r['path'] == path for r in handoff['artifacts']), 'Collector handoff must hash its package artifact')
