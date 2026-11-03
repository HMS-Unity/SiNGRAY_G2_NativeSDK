"""Prepare a local review copy and inventories; never publish or build a release.

Run from any directory with Python 3. Output stays under ignored Builds.
The main Assets, Packages, ProjectSettings and Git index are read-only inputs.
"""
from pathlib import Path
import csv
import hashlib
import json
import re
import shutil
import subprocess
import tarfile
import zipfile
from collections import Counter
import argparse

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Builds/SDKDelivery_20261103'
REVIEW = OUT / 'ReviewFiles'
DOCS = ROOT / 'Docs/Delivery'


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def write_csv(path, rows, fields):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open('w', encoding='utf-8-sig', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)


def group(name):
    # Metadata follows the same decision as the corresponding asset/folder.
    name = name.removesuffix('.meta')
    if any(name == p or name.startswith(p + '/') for p in (
        'Assets/XRFoundation/SampleScenes/Wifi',
        'Assets/XRFoundation/SampleScenes/Bluetooth',
        'Assets/XRFoundation/Core/Foundation/Bluetooth',
        'Assets/XRFoundation/Core/Foundation/Wifi',
    )):
        return 'LOCAL_ONLY', 'Never publish dedicated connectivity modules'
    if '/Tests/' in name or '/SingrayG2Validation' in name:
        return 'INTERNAL', 'Development validation, not runtime initialization'
    if name.startswith('Assets/XRFoundation/Core/Editor/'):
        base = Path(name).name
        if base in ('XvXRFoundation.cs', 'SDKLayerSetup.cs'):
            return 'CUSTOMER_SETUP', 'Keep required scene and layer setup'
        if base == 'BuildSettingWidows.cs':
            return 'REVIEW_HOLD', 'Legacy setup writes API 26/27; source remains unchanged'
        if base == 'ProjectBuild.cs':
            return 'REVIEW_HOLD', 'Shared sample builds include local-only entries; review publication hunks'
        if 'Validation' in base or base in ('SdkSafetyChecks.cs', 'BuildVerification.cs'):
            return 'INTERNAL', 'Development validation/build verification'
        return 'OPTIONAL_EDITOR', 'Optional export/build utility; review dependencies before omission'
    if name in ('Packages/manifest.json', 'Packages/packages-lock.json'):
        return 'REVIEW_HOLD', 'Review copy removes internal MCP and prunes its dependency graph'
    if name.startswith('ProjectSettings/'):
        return 'REFERENCE_CONFIG', 'Reference only; merge settings and use customer signing/identity'
    if name.startswith('Assets/Plugins/Android/') or name.endswith('/AndroidConnection.cs'):
        return 'REVIEW_HOLD', 'Shared Android integration; retain required chain and review excluded feature changes'
    if name.startswith('Assets/XRFoundation/SampleScenes/'):
        if '/SpeechVoice/' in name or '/PlaneDetection/' in name:
            return 'COMPATIBILITY_SAMPLE', 'Retain references only; not an accepted primary entry in this pass'
        return 'OPTIONAL_SAMPLE', 'Integration example; keep scene/prefab dependency closure'
    if name.startswith('Packages/MixedReality/'):
        return 'PACKAGE_DEPENDENCY', 'Local MRTK 2.8.3 package archive'
    if name.startswith('Assets/XRFoundation/ThirdParty/'):
        return 'THIRD_PARTY', 'Retain required assets, importer metadata and notices'
    if name.startswith('Assets/XRFoundation/Core/'):
        return 'SDK_RUNTIME', 'Conservative runtime integration set; not a proven minimal subset'
    if name.startswith('Assets/Singray/'):
        return 'SAMPLE_SUPPORT', 'Branding referenced by sample assets; inspect dependency closure'
    return 'INTEGRATION_SUPPORT', 'Preserve referenced assets, folders and metadata'


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    DOCS.mkdir(parents=True, exist_ok=True)
    # Avoid stale evidence when the tool is rerun; never delete an output tree.
    if REVIEW.exists():
        raise SystemExit('ReviewFiles already exists. Preserve it; choose/archive the prior review before rerunning.')
    inputs = sorted(p for folder in ('Assets', 'Packages', 'ProjectSettings')
                    for p in (ROOT / folder).rglob('*') if p.is_file())
    hashes = []
    texts = {}
    guids = {}
    for path in inputs:
        name = path.relative_to(ROOT).as_posix()
        category, reason = group(name)
        digest = sha(path)
        hashes.append(dict(path=name, category=category, reason=reason,
                           bytes=path.stat().st_size, sha256=digest))
        if path.suffix in ('.meta', '.cs', '.prefab', '.unity', '.asset', '.mat', '.shader', '.xml'):
            text = path.read_text(encoding='utf-8-sig', errors='replace')
            texts[name] = text
            if path.suffix == '.meta':
                match = re.search(r'^guid: ([a-f0-9]{32})', text, re.M)
                if match:
                    guids.setdefault(match[1], []).append(name[:-5])
    write_csv(OUT / 'file-inventory.csv', hashes, ['path', 'category', 'reason', 'bytes', 'sha256'])
    grouped = []
    for category in sorted({row['category'] for row in hashes}):
        subset = [row for row in hashes if row['category'] == category]
        if category == 'LOCAL_ONLY':
            continue  # Detailed local module inventory stays in ignored evidence.
        grouped.append(dict(category=category, files=len(subset), bytes=sum(row['bytes'] for row in subset),
                            examplePath=subset[0]['path'], decision=subset[0]['reason']))
    write_csv(DOCS / 'File-Groups.csv', grouped, ['category', 'files', 'bytes', 'examplePath', 'decision'])
    primary = {
        'Viewer': ('Viewer', 'CameraDemo', 'User reported no observed issues; per-scenario records remain separate'),
        'Rgbd': ('Rgbd', 'RgbdDemo', 'User reported no major functional issue'),
        'TofPointCloud': ('TofPointCloud', '', 'User reported acceptance; sustained shared-path regression not established'),
        'MRVideoCapture': ('MRVideoCapture', 'MRVideoCaptureDemo', 'User reported normal operation'),
        'MediaRecorder': ('MediaRecorder', 'MediaRecorderDemo', 'Build/install evidence; full recording/audio/file acceptance not established here'),
        'SpatialMap': ('SpatialMap', 'SpatialMapDemo', 'User reported no major issue; prior native reset risk retained'),
        'SpatialMesh': ('SpatialMesh', 'SpatialMeshDemo', 'User reported acceptance'),
        'TagRecognizer': ('TagRecognizer', 'TagRecognizerDemo', 'User reported acceptance'),
        'StaticGesture': ('StaticGesture', 'StaticGestureDemo', 'User reported acceptance'),
        'KeyInput': ('KeyInput', 'KeyInputDemo', 'Build/install evidence; physical per-scenario acceptance not established here'),
        'Keyboard': ('Keyboard', 'KeyBoardDemo', 'English default changed; full post-replacement device acceptance not established here'),
        'Gaze': ('Gaze', '', 'Build/install evidence; device acceptance not established here'),
        'MRTK2': ('MRTK2', '', 'User acceptance 2026-11-03; no observed issues'),
        'IRToWorld': ('IRToWorld', '', 'User acceptance 2026-11-03; no observed issues'),
        'SystemSetting': ('SystemSetting', 'XvSystemSettingDemo', 'IPD deferred; ELC hardware acceptance pending; partial feature validation'),
        'RTSPStreamer': ('RTSPStreamer', '', 'Playback failed; investigation deferred'),
        'SDKSampleHub': ('SDKSamples', '', 'Optional multi-feature host; not rebuilt or accepted as a whole in this task'),
    }
    samples = []
    for folder, (scene, prefab, status) in primary.items():
        scene_path = f'Assets/XRFoundation/SampleScenes/{folder}/Scenes/{scene}.unity'
        assert (ROOT / scene_path).is_file(), scene_path
        prefab_path = f'Assets/XRFoundation/SampleScenes/{folder}/Prefabs/{prefab}.prefab' if prefab else ''
        if prefab_path:
            assert (ROOT / prefab_path).is_file(), prefab_path
        samples.append(dict(module=folder, scene=scene_path, contentPrefab=prefab_path,
                            acceptance=status, layout='Preserved; structural consolidation deferred'))
    write_csv(DOCS / 'Sample-Entries.csv', samples, ['module', 'scene', 'contentPrefab', 'acceptance', 'layout'])
    native = []
    archive_details = []
    for row in hashes:
        path = ROOT / row['path']
        if path.suffix not in ('.aar', '.so', '.dll', '.tgz', '.jar'):
            continue
        pointer = path.read_bytes()[:100].startswith(b'version https://git-lfs.github.com/spec/v1')
        if pointer:
            raise RuntimeError('LFS pointer instead of binary: ' + row['path'])
        native.append({key: row[key] for key in ('path', 'bytes', 'sha256', 'category')})
        detail = dict(path=row['path'], nativeLibraries=[], noticeFiles=[])
        if path.suffix == '.aar':
            with zipfile.ZipFile(path) as archive:
                detail['nativeLibraries'] = [n for n in archive.namelist() if n.endswith('.so')]
                detail['noticeFiles'] = [n for n in archive.namelist() if re.search('license|notice|copying', n, re.I)]
                detail['hasClassesJar'] = 'classes.jar' in archive.namelist()
        if path.suffix == '.tgz':
            with tarfile.open(path) as archive:
                detail['noticeFiles'] = [m.name for m in archive if re.search('license|notice|copying', m.name, re.I)]
        archive_details.append(detail)
    write_csv(DOCS / 'Native-Dependencies.csv', native, ['path', 'bytes', 'sha256', 'category'])
    (OUT / 'native-archive-contents.json').write_text(json.dumps(archive_details, indent=2))

    packages = json.loads((ROOT / 'Packages/manifest.json').read_text())
    lock = json.loads((ROOT / 'Packages/packages-lock.json').read_text())
    package_rows = [dict(package=k, version=v, scope=('INTERNAL' if k == 'com.coplaydev.unity-mcp' else 'CURRENT_DIRECT_DEPENDENCY'))
                    for k, v in packages['dependencies'].items()]
    write_csv(DOCS / 'Package-Dependencies.csv', package_rows, ['package', 'version', 'scope'])
    filtered = {**packages, 'dependencies': {k: v for k, v in packages['dependencies'].items() if k != 'com.coplaydev.unity-mcp'}}
    reachable = set()
    queue = list(filtered['dependencies'])
    while queue:
        name = queue.pop()
        if name in reachable:
            continue
        reachable.add(name)
        queue.extend(lock['dependencies'].get(name, {}).get('dependencies', {}))
    filtered_lock = {**lock, 'dependencies': {k: v for k, v in lock['dependencies'].items() if k in reachable}}

    resources = []
    for source, text in texts.items():
        if not source.endswith('.cs') or group(source)[0] in ('LOCAL_ONLY', 'INTERNAL'):
            continue
        for match in re.finditer(r'Resources\.Load(?:<[^>]+>)?\(\s*"([^"]+)"', text):
            key = match[1]
            matches = []
            for asset in inputs:
                n = asset.relative_to(ROOT).as_posix()
                if '/Resources/' in n and asset.suffix != '.meta':
                    resource_path = n.split('/Resources/', 1)[1]
                    if str(Path(resource_path).with_suffix('')).replace('\\', '/') == key:
                        matches.append(n)
            resources.append(dict(source=source, key=key, matches=';'.join(matches),
                                  status='LOCAL_MATCH' if matches else 'PACKAGE_DYNAMIC_OR_UNRESOLVED'))
    write_csv(OUT / 'resource-lookups.csv', resources, ['source', 'key', 'matches', 'status'])

    edges = []
    for source, text in texts.items():
        if Path(source).suffix not in ('.prefab', '.unity', '.asset', '.mat'):
            continue
        if group(source)[0] in ('LOCAL_ONLY', 'INTERNAL'):
            continue
        for guid in set(re.findall(r'guid: ([a-f0-9]{32})', text)):
            for target in guids.get(guid, []):
                target_group = group(target)[0]
                if target_group in ('LOCAL_ONLY', 'INTERNAL') or (group(source)[0] == 'SDK_RUNTIME' and target_group == 'OPTIONAL_SAMPLE'):
                    edges.append(dict(source=source, target=target, targetCategory=target_group, guid=guid))
    write_csv(OUT / 'dependency-review.csv', sorted(edges, key=lambda e: (e['source'], e['target'])), ['source', 'target', 'targetCategory', 'guid'])
    shared = [edge for edge in edges if edge['targetCategory'] == 'OPTIONAL_SAMPLE']
    write_csv(DOCS / 'Shared-Asset-Dependencies.csv', sorted(shared, key=lambda e: e['source']), ['source', 'target', 'targetCategory', 'guid'])
    duplicates = {g: names for g, names in guids.items() if len(names) > 1}
    (OUT / 'duplicate-local-guids.json').write_text(json.dumps(duplicates, indent=2))

    copied = []
    for row in hashes:
        if row['category'] in ('LOCAL_ONLY', 'INTERNAL'):
            continue
        name = row['path']
        destination = REVIEW / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        if name == 'Packages/manifest.json':
            destination.write_text(json.dumps(filtered, indent=2) + '\n')
        elif name == 'Packages/packages-lock.json':
            destination.write_text(json.dumps(filtered_lock, indent=2) + '\n')
        else:
            shutil.copy2(ROOT / name, destination)
            assert sha(destination) == row['sha256'], name
        copied.append(dict(path=name, category=row['category'], bytes=destination.stat().st_size,
                           sha256=sha(destination), transformed=name in ('Packages/manifest.json', 'Packages/packages-lock.json')))
    write_csv(OUT / 'review-copy-manifest.csv', copied, ['path', 'category', 'bytes', 'sha256', 'transformed'])
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        assert all(group(p.relative_to(REVIEW).as_posix())[0] not in ('LOCAL_ONLY', 'INTERNAL')
                   for p in (REVIEW / folder).rglob('*') if p.is_file())
    assert 'com.coplaydev.unity-mcp' not in json.dumps(filtered_lock)
    assert all(sha(ROOT / row['path']) == row['sha256'] for row in hashes), 'Inputs changed during review'
    branch = subprocess.check_output(['git', 'branch', '--show-current'], cwd=ROOT, text=True).strip()
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    summary = dict(branch=branch, baseCommit=head, snapshot='working-tree-with-unpublished-changes',
                   inputFiles=len(hashes), reviewFiles=len(copied), categories=dict(Counter(r['category'] for r in hashes)),
                   nativeAndPackageFiles=len(native), resourceLiteralLookups=len(resources),
                   dependencyReviewEdges=len(edges), duplicateLocalGuids=len(duplicates),
                   integrity='PASS', releaseReady=False, cleanProjectImport='NOT_RUN', hardwareTest='NOT_RUN',
                   transformedFiles=['Packages/manifest.json', 'Packages/packages-lock.json'])
    (OUT / 'summary.json').write_text(json.dumps(summary, indent=2))
    (REVIEW / 'REVIEW-ONLY.txt').write_text(
        'Development review copy, not a frozen customer release.\n'
        'Original paths and metadata are preserved. Dedicated local-only modules and internal tests are omitted.\n'
        'Shared REVIEW_HOLD files still require scoped publication review. Reference ProjectSettings must not overwrite customer settings.\n'
        'Only package manifest/lock were transformed to remove internal MCP. Clean import/build/device validation is NOT RUN.\n'
        'Use Docs/Delivery in the repository and the adjacent CSV/JSON evidence before selecting release inputs.\n')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', default='Builds/SDKDelivery_20261103', help='New local review directory under Builds')
    args = parser.parse_args()
    OUT = (ROOT / args.output).resolve()
    if not OUT.is_relative_to((ROOT / 'Builds').resolve()):
        raise SystemExit('Output must stay under this repository Builds directory.')
    REVIEW = OUT / 'ReviewFiles'
    main()
