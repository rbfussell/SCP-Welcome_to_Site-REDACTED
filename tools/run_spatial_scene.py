"""Build/run the Godot .NET spatial prototype without renaming the source workspace."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def run(command, cwd, env, log=None):
    if log:
        result = subprocess.run(command, cwd=cwd, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        log.write_text(result.stdout)
        if result.returncode:
            print(result.stdout)
        result.check_returncode()
        return result.stdout
    subprocess.run(command, cwd=cwd, env=env, check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Run the engine-independent spatial checks, then exit.')
    parser.add_argument('--smoke', type=Path, metavar='PNG', help='Run scripted Godot input/render checks and save a screenshot.')
    parser.add_argument('--operation-smoke', type=Path, metavar='PNG', help='Run the orders/OSCP input and render fixture.')
    parser.add_argument('--inventory-smoke', type=Path, metavar='PNG', help='Run realtime packing, rotation, nested inspection and inventory input/render checks.')
    parser.add_argument('--pack-pocket-smoke', type=Path, metavar='PNG', help='Run only worn backpack main/side segment input and closure checks.')
    parser.add_argument('--pocket-smoke', type=Path, metavar='PNG', help='Run paperdoll jacket/trouser/backpack pocket segments, coverage, paid opening and storage checks.')
    parser.add_argument('--worn-storage-smoke', type=Path, metavar='PNG', help='Run worn bag/pouch sprite storage targets and normal packing checks.')
    parser.add_argument('--sprite-drop-smoke', type=Path, metavar='PNG', help='Run free sprite body fitting, exposed mounts and worn drag-source checks.')
    parser.add_argument('--ground-smoke', type=Path, metavar='PNG', help='Run current-tile floor stacking, clothes staging and pickup checks.')
    parser.add_argument('--transfer-smoke', type=Path, metavar='PNG', help='Run independent source/destination inventory transfer and access checks.')
    parser.add_argument('--hook-smoke', type=Path, metavar='PNG', help='Run only the native placement-extender and nested-mount checks.')
    parser.add_argument('--paperdoll-smoke', type=Path, metavar='PNG', help='Run upper-body sprite alpha, layering, identity and paid fastening checks.')
    parser.add_argument('--body-drop-smoke', type=Path, metavar='PNG', help='Run native body fitting, exposed-equipment inspection and body-source drag checks.')
    parser.add_argument('--combat-smoke', type=Path, metavar='PNG', help='Run tactical input, clocks, save and journal render checks.')
    parser.add_argument('--mode', choices=('local', 'oscp', 'phone'), default='local', help='Launch local play or standalone OSCP with a workstation/phone profile.')
    parser.add_argument('--oscp-smoke', type=Path, metavar='PNG', help='Run standalone CRT/phone command, lifecycle and render checks.')
    parser.add_argument('--godot', help='Godot .NET executable (defaults to godot-dotnet).')
    args = parser.parse_args()
    oscp_smoke = args.oscp_smoke
    if oscp_smoke:
        args.mode = "oscp"
    if args.mode != 'local' and any((args.smoke, args.operation_smoke, args.combat_smoke, args.inventory_smoke, args.pocket_smoke, args.pack_pocket_smoke, args.worn_storage_smoke, args.sprite_drop_smoke, args.ground_smoke, args.transfer_smoke, args.hook_smoke, args.body_drop_smoke, args.paperdoll_smoke)):
        parser.error('Use --oscp-smoke for standalone device checks, or --mode local for spatial checks.')
    operation_smoke = args.operation_smoke
    combat_smoke = args.combat_smoke
    if sum(bool(v) for v in (args.check, args.smoke, args.operation_smoke, args.combat_smoke, args.oscp_smoke, args.inventory_smoke, args.pocket_smoke, args.pack_pocket_smoke, args.worn_storage_smoke, args.sprite_drop_smoke, args.ground_smoke, args.transfer_smoke, args.hook_smoke, args.body_drop_smoke, args.paperdoll_smoke)) > 1:
        parser.error('Choose one check or smoke mode.')
    args.smoke = args.smoke or args.operation_smoke or args.combat_smoke or args.oscp_smoke or args.inventory_smoke or args.pocket_smoke or args.pack_pocket_smoke or args.worn_storage_smoke or args.sprite_drop_smoke or args.ground_smoke or args.transfer_smoke or args.hook_smoke or args.body_drop_smoke or args.paperdoll_smoke
    if not shutil.which('dotnet'):
        raise SystemExit('Install the .NET SDK selected in global.json first.')
    godot = args.godot or shutil.which('godot-dotnet')
    if not args.check and not godot:
        raise SystemExit('Use --godot /path/to/Godot_4.7.1_mono, or put godot-dotnet on PATH.')

    inspection = ROOT / 'work/preview/spatial-scene'
    inspection.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    # Isolate tool caches without moving the user's HOME or changing global configuration.
    env.setdefault('NUGET_PACKAGES', str(Path.home() / '.nuget/packages'))
    env['DOTNET_CLI_HOME'] = str(inspection / 'dotnet')
    env['DOTNET_SKIP_FIRST_TIME_EXPERIENCE'] = '1'
    env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    env['XDG_CONFIG_HOME'] = str(inspection / 'config')
    env['XDG_CACHE_HOME'] = str(inspection / 'cache')
    env['XDG_DATA_HOME'] = str(ROOT / 'user-data')

    # Linux .NET app-host/search paths and MSBuild globs fail on this workspace's
    # colon/brackets. A symlink is insufficient: the host resolves its real path.
    # Only the small prototype is copied, never the wiki archive or Git history.
    with tempfile.TemporaryDirectory(prefix='scp-spatial-') as directory:
        stage = Path(directory)
        for name in ('game', 'src', 'tests'):
            shutil.copytree(ROOT / name, stage / name, ignore=shutil.ignore_patterns('.godot', 'bin', 'obj', '*.uid', '*.import'))
        shutil.copy2(ROOT / 'global.json', stage / 'global.json')
        common = ['-p:NuGetAudit=false', '--verbosity', 'minimal']
        if args.check:
            project = stage / 'tests/SpatialChecks/SpatialChecks.csproj'
            run(['dotnet', 'build', str(project), '--ignore-failed-sources', *common], stage, env)
            run(['dotnet', str(stage / 'tests/SpatialChecks/bin/Debug/net8.0/SpatialChecks.dll'), str(stage / 'game/Content/spatial-facility.json')], stage, env)
            return
        version = subprocess.check_output([godot, '--version'], env=env, text=True).strip()
        if not version.startswith('4.7.1.') or '.mono.' not in version:
            raise SystemExit(f'This prototype is qualified for Godot .NET 4.7.1; found {version}.')
        run(['dotnet', 'build', str(stage / 'game/SiteScene.csproj'), '--ignore-failed-sources', *common], stage, env)
        print('Importing prototype assets...', flush=True)
        log = run([godot, '--headless', '--editor', '--path', str(stage / 'game'), '--import', '--quit'], stage, env, inspection / 'last-import.log')
        if 'ERROR:' in log:
            raise SystemExit('Godot reported import errors; see work/preview/spatial-scene/last-import.log.')
        command = [godot, '--path', str(stage / 'game')]
        if args.mode != 'local':
            command += ['res://Scenes/OscpScene.tscn']
        if args.smoke:
            output = args.smoke.resolve()
            output.parent.mkdir(parents=True, exist_ok=True)
            command += ['--audio-driver', 'Dummy', '--', '--oscp-smoke' if oscp_smoke else '--scene-smoke', str(output)]
            if args.inventory_smoke or args.pocket_smoke or args.pack_pocket_smoke or args.worn_storage_smoke or args.sprite_drop_smoke or args.ground_smoke or args.transfer_smoke or args.hook_smoke or args.body_drop_smoke or args.paperdoll_smoke:
                command += ['--inventory-smoke']
            if args.pack_pocket_smoke:
                command += ['--pack-pocket-smoke']
            if args.pocket_smoke:
                command += ['--pocket-smoke']
            if args.worn_storage_smoke:
                command += ['--worn-storage-smoke']
            if args.sprite_drop_smoke:
                command += ['--sprite-drop-smoke']
            if args.ground_smoke:
                command += ['--ground-smoke']
            if args.transfer_smoke:
                command += ['--transfer-smoke']
            if args.hook_smoke:
                command += ['--hook-smoke']
            if args.paperdoll_smoke:
                command += ['--paperdoll-smoke']
            if args.body_drop_smoke:
                command += ['--body-drop-smoke']
            if combat_smoke:
                command += ['--combat-smoke']
            if operation_smoke:
                command += ['--operation-smoke']
            if shutil.which('xvfb-run'):
                command = ['xvfb-run', '-a', '-s', '-screen 0 1440x900x24', *command]
        if args.mode == 'phone':
            command += (['--'] if '--' not in command else []) + ['--field-phone']
        print('Starting selected scene. Changes should be edited in the source workspace.', flush=True)
        run(command, stage, env)


if __name__ == '__main__':
    main()
