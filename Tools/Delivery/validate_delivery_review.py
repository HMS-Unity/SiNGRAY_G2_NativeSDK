"""Verify the existing local review snapshot and copy the selected documentation.

This performs file/reference checks only; it does not run Unity or a device.
"""
from pathlib import Path
import csv
import hashlib
import json
import re
import shutil
import argparse

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Builds/SDKDelivery_20261103'
REVIEW = OUT / 'ReviewFiles'


def rows(path):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    checks = 0
    for row in rows(OUT / 'file-inventory.csv'):
        assert digest(ROOT / row['path']) == row['sha256'], row['path']
        checks += 1
    for row in rows(OUT / 'review-copy-manifest.csv'):
        assert digest(REVIEW / row['path']) == row['sha256'], row['path']
        checks += 1
    for row in rows(ROOT / 'Docs/Delivery/Sample-Entries.csv'):
        assert (REVIEW / row['scene']).is_file(), row
        if row['contentPrefab']:
            assert (REVIEW / row['contentPrefab']).is_file(), row
        checks += 1
    for row in rows(ROOT / 'Docs/Delivery/Shared-Asset-Dependencies.csv'):
        assert (REVIEW / row['source']).is_file(), row
        assert (REVIEW / row['target']).is_file(), row
        assert (REVIEW / (row['target'] + '.meta')).is_file(), row
        checks += 1
    manifest = json.loads((REVIEW / 'Packages/manifest.json').read_text())
    lock = json.loads((REVIEW / 'Packages/packages-lock.json').read_text())
    assert 'com.coplaydev.unity-mcp' not in manifest['dependencies']
    assert 'com.coplaydev.unity-mcp' not in lock['dependencies']
    assert all(name in lock['dependencies'] for name in manifest['dependencies'])
    for path in REVIEW.rglob('*'):
        if path.is_file():
            assert path.suffix not in ('.keystore', '.jks', '.apk', '.aab'), path
            assert '/SampleScenes/Wifi/' not in path.as_posix(), path
            assert '/SampleScenes/Bluetooth/' not in path.as_posix(), path
    assert not (REVIEW / 'Library').exists()
    checks += 7

    selected = [ROOT / name for name in ('README.md', 'RELEASE_NOTES.md',
                'Docs/API-Reference.md', 'Docs/Demo-and-User-Guide.md', 'Docs/Keys-and-Buttons.md')]
    selected += [p for p in (ROOT / 'Docs/Delivery').rglob('*') if p.is_file()]
    selected += [p for p in (ROOT / 'Docs/images').rglob('*') if p.is_file()]
    docs = []
    for path in selected:
        name = path.relative_to(ROOT).as_posix()
        destination = REVIEW / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, destination)
        assert digest(path) == digest(destination)
        docs.append(dict(path=name, bytes=path.stat().st_size, sha256=digest(path)))
        checks += 1
    for path in (ROOT / 'Docs/Delivery').glob('*.md'):
        text = path.read_text()
        assert not re.search(r'[\u3400-\u9fff]', text), path
        for target in re.findall(r'\[[^\]]+\]\(([^)]+)\)', text):
            if '://' not in target and not target.startswith('#'):
                assert (path.parent / target.split('#')[0]).is_file(), (path, target)
                checks += 1
    (OUT / 'documentation-manifest.json').write_text(json.dumps(docs, indent=2))
    result = dict(result='PASS', fileAndDocumentChecks=checks,
                  mainAssetPackageSettingsHashesUnchanged=True,
                  selectedDocsCopied=len(docs), nativeInputs=len(rows(ROOT / 'Docs/Delivery/Native-Dependencies.csv')),
                  cleanProjectImport='NOT_RUN', unityBuild='NOT_RUN', deviceTest='NOT_RUN',
                  remaining='Release holds and clean-project acceptance are in Docs/Delivery/Delivery-Checklist.md')
    (OUT / 'validation.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', default='Builds/SDKDelivery_20261103')
    args = parser.parse_args()
    OUT = (ROOT / args.output).resolve()
    if not OUT.is_relative_to((ROOT / 'Builds').resolve()):
        raise SystemExit('Output must stay under this repository Builds directory.')
    REVIEW = OUT / 'ReviewFiles'
    main()
