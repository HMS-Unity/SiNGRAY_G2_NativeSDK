"""Audit repository text, including escaped Unity strings and legacy encodings.

Functional language data is retained only through reviewed, hash-bound exemptions.
This checks authored repository text, not strings inside third-party binaries.
"""
import argparse
import hashlib
import io
import html
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
TEXT_EXTENSIONS = {
    '.cs', '.java', '.cpp', '.c', '.h', '.hpp', '.shader', '.hlsl', '.cginc',
    '.compute', '.md', '.txt', '.rst', '.json', '.yml', '.yaml', '.unity',
    '.prefab', '.asset', '.mat', '.meta', '.xml', '.html', '.htm', '.uxml',
    '.uss', '.js', '.ts', '.py', '.sh', '.cmd', '.bat', '.gradle', '.properties',
    '.asmdef', '.asmref', '.csv', '.tsv', '.resx', '.config', '.plist', '.cmake',
}
TEXT_NAMES = {'.gitignore', '.gitattributes', 'CMakeLists.txt'}
BINARY_EXTENSIONS = {'.aar', '.tgz', '.zip', '.dll', '.so', '.a', '.ttf', '.woff', '.woff2', '.png', '.jpg', '.jpeg', '.gif', '.wav', '.mp3', '.mp4', '.p7s'}
UNICODE_ESCAPE = re.compile(r'\\(?:u([0-9a-fA-F]{4})|U([0-9a-fA-F]{8}))')
SCRIPT_RANGES = (
    (0x3400, 0x4DBF), (0x4E00, 0x9FFF), (0xF900, 0xFAFF),
    (0x20000, 0x2FA1F), (0x30000, 0x323AF),
    (0x3040, 0x30FF), (0x3100, 0x312F), (0x31A0, 0x31BF),
    (0x31F0, 0x31FF), (0xAC00, 0xD7AF),
)


SCRIPT_PATTERN = re.compile('[' + ''.join(chr(low) + '-' + chr(high) for low, high in SCRIPT_RANGES) + ']')


def contains_language_characters(text):
    return bool(SCRIPT_PATTERN.search(text))


def visible_text(text):
    def decode(match):
        value = int(match[1] or match[2], 16)
        return chr(value) if value <= 0x10FFFF else match[0]
    return html.unescape(UNICODE_ESCAPE.sub(decode, text))


def read_text(data):
    encodings = ['utf-16'] if data.startswith((b'\xff\xfe', b'\xfe\xff')) else ['utf-8-sig', 'gb18030']
    for encoding in encodings:
        try:
            text = data.decode(encoding)
            if '\0' not in text[:1024]:
                return text, encoding
        except UnicodeError:
            pass
    raise ValueError('Cannot decode this expected text file without loss.')


def exempt_lines(entry, lines):
    kind = entry['kind']
    if kind == 'whole-file':
        return set(range(len(lines)))
    selected = set()
    inside = False
    speech_field = False
    for i, line in enumerate(lines):
        if kind == 'font-creation-sequence':
            if re.match(r'^    characterSequence:', line):
                inside = True
            elif inside and re.match(r'^    \S', line):
                inside = False
            if inside:
                selected.add(i)
        elif kind == 'speech-word-fields' and re.match(r'^\s+word:', line):
            selected.add(i)
        elif kind == 'speech-word-overrides':
            if 'propertyPath:' in line:
                speech_field = bool(re.search(r'aitalkWords\.Array\.data\[\d+\]\.word\s*$', line))
            elif speech_field and re.match(r'^\s+value:', line):
                selected.add(i)
                speech_field = False
        elif kind == 'unicode-range-pattern':
            # This bounds expression is validation code, not displayed text.
            if 'assert not re.search(' in line and 'text), path' in line:
                selected.add(i)
    return selected


def approved_hash(data):
    # Git may check identical tracked text out as CRLF on Windows and LF elsewhere.
    return hashlib.sha256(data.replace(b'\r\n', b'\n')).hexdigest()


def audit(root, paths, manifest):
    failures = []
    exemptions = {}
    reviewed_data = []
    for entry in manifest['entries']:
        path = root / entry['path']
        if not path.is_file():
            failures.append({'path': entry['path'], 'reason': 'Approved functional data is missing.'})
            continue
        digest = approved_hash(path.read_bytes())
        if digest != entry['sha256']:
            failures.append({'path': entry['path'], 'reason': 'Functional data changed; review its exemption explicitly.'})
            continue
        exemptions[entry['path']] = entry
        reviewed_data.append({'path': entry['path'], 'kind': entry['kind'], 'sha256': digest, 'reason': entry['reason']})

    checked = 0
    excluded = 0
    encodings = {}
    filename_checks = 0
    pdf_documents = []
    for name in sorted(set(paths)):
        path = (root / name).resolve()
        if not path.is_relative_to(root):
            failures.append({'path': name, 'reason': 'Path leaves the repository.'})
            continue
        filename_checks += 1
        if contains_language_characters(name):
            failures.append({'path': name, 'reason': 'Non-English filename.'})
        if not path.is_file():
            failures.append({'path': name, 'reason': 'Tracked file is missing.'})
            continue
        if path.suffix.lower() in BINARY_EXTENSIONS:
            excluded += 1
            continue
        data = path.read_bytes()
        if path.suffix.lower() == '.pdf':
            try:
                from pypdf import PdfReader
                reader = PdfReader(io.BytesIO(data))
                text = '\n'.join(page.extract_text() or '' for page in reader.pages)
                if not text.strip():
                    raise ValueError('PDF has no extractable text; perform a separate visual/OCR review.')
                encoding = 'pdf-extracted-text'
                pdf_documents.append({'path': name, 'pages': len(reader.pages), 'textCharacters': len(text)})
            except ImportError:
                failures.append({'path': name, 'reason': 'Install pypdf to include PDF documentation in the repository audit.'})
                continue
            except Exception as error:
                failures.append({'path': name, 'reason': 'PDF text audit failed: ' + str(error)})
                continue
        else:
            try:
                text, encoding = read_text(data)
            except ValueError as error:
                if path.suffix.lower() in TEXT_EXTENSIONS or path.name in TEXT_NAMES:
                    failures.append({'path': name, 'reason': str(error)})
                else:
                    excluded += 1
                continue
        checked += 1
        encodings[encoding] = encodings.get(encoding, 0) + 1
        lines = text.splitlines()
        allowed = exempt_lines(exemptions[name], lines) if name in exemptions else set()
        for i, line in enumerate(lines):
            if i not in allowed and contains_language_characters(visible_text(line)):
                failures.append({'path': name, 'line': i + 1, 'reason': 'Non-English text outside approved functional data.'})

    return {
        'status': 'PASS' if not failures else 'FAIL',
        'trackedPathsChecked': filename_checks,
        'textFilesChecked': checked,
        'binaryOrNontextPathsNotDecoded': excluded,
        'encodingsRead': encodings,
        'pdfDocumentsChecked': pdf_documents,
        'reviewedFunctionalAndTechnicalData': reviewed_data,
        'approvalHashFormat': manifest.get('hashFormat'),
        'violations': failures,
        'limits': 'Decodable repository text, serialized strings and PDF text; image-only text and vendor binary/archive contents are not translated. Approved functional language data is retained.',
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=pathlib.Path, default=ROOT)
    parser.add_argument('--manifest', type=pathlib.Path)
    parser.add_argument('--report', type=pathlib.Path)
    parser.add_argument('--paths', nargs='+', help='Check explicit repository-relative paths instead of all tracked paths.')
    args = parser.parse_args()
    root = args.root.resolve()
    manifest_path = args.manifest or root / 'Tools/Validation/sdk_language_data.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    paths = args.paths or subprocess.check_output(['git', 'ls-files', '-z'], cwd=root).decode('utf-8').split('\0')
    result = audit(root, [p for p in paths if p], manifest)
    result['scanMode'] = 'explicit-paths' if args.paths else 'complete-tracked-repository'
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    summary = {k: v for k, v in result.items() if k != 'reviewedFunctionalAndTechnicalData'}
    summary['approvedDataEntries'] = len(result['reviewedFunctionalAndTechnicalData'])
    print(json.dumps(summary, indent=2))
    return 0 if result['status'] == 'PASS' else 1


if __name__ == '__main__':
    sys.exit(main())
