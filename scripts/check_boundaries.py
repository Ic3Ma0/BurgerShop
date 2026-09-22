#!/usr/bin/env python3
"""Small, explicit dependency guard for the extracted offline rules, not a C# parser."""
import argparse
import json
from pathlib import Path
import re
import subprocess
import sys

RULES = Path('Assets/_Project/Scripts/Economy/Rules')
ASSEMBLY = 'BurgerShop.Economy.Rules'
REVIEW_SECTION = '## 技术边界与方案审查'
REVIEW_ITEMS = ('归属与复用', '状态归属', '依赖方向', '兼容与失败', '自动防线')


def code_only(source):
    # Remove comments/literals so documentation and diagnostics don't trip dependencies.
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"', '', source)


def check_rules(root):
    errors = []
    rules = root / RULES
    expected = rules / (ASSEMBLY + '.asmdef')
    definitions = list(rules.rglob('*.asmdef'))
    if definitions != [expected] or list(rules.rglob('*.asmref')):
        errors.append('Rules must have exactly its own asmdef and no nested assembly/asmref escape.')
    try:
        definition = json.loads(expected.read_text())
        contract = {'name': ASSEMBLY, 'noEngineReferences': True, 'references': [],
                    'overrideReferences': True, 'precompiledReferences': []}
        for key, value in contract.items():
            if definition.get(key) != value:
                errors.append(f'{expected}: {key} must be {value!r}')
        if definition.get('allowUnsafeCode', False):
            errors.append('Rules cannot enable unsafe code.')
    except (OSError, ValueError) as exc:
        errors.append(f'Cannot read rules assembly: {exc}')
    sources = list(rules.rglob('*.cs'))
    if not sources:
        errors.append('Rules assembly has no sources.')
    banned = (
        r'\bUnity(?:Engine|Editor)\b',
        r'\bBurgerShop\s*\.\s*(?!Economy\b)\w+',
        r'\bSystem\s*\.\s*(?:IO|Net|Reflection|Threading|Diagnostics)\b',
        r'\b(?:Environment|AppDomain|Activator|Random)\b',
        r'\b(?:DateTime|DateTimeOffset)\s*\.\s*(?:Now|UtcNow|Today)\b',
        r'\bDllImport\b',
    )
    for path in sources:
        code = code_only(path.read_text())
        for pattern in banned:
            if re.search(pattern, code):
                errors.append(f'{path.relative_to(root)}: forbidden dependency/ambient input: {pattern}')
        # Aliases could conceal a clock or reflection call from the lightweight scan.
        if re.search(r'\busing\s+(?:\w+\s*=|static\s+)', code):
            errors.append(f'{path.relative_to(root)}: aliases/static imports can hide ambient inputs; use explicit types.')
    return errors


def check_spec(path):
    try:
        text = path.read_text()
    except OSError as exc:
        return [str(exc)]
    if REVIEW_SECTION not in text:
        return [f'{path}: missing {REVIEW_SECTION}']
    section = text.split(REVIEW_SECTION, 1)[1].split('\n## ', 1)[0]
    return [f'{path}: review must explain {item}' for item in REVIEW_ITEMS if item not in section]


def check_change_review(root, base):
    # CI checks the submitted diff; local work can use --spec before committing.
    diff = subprocess.check_output(['git', 'diff', '--name-only', '-z', base, 'HEAD'], cwd=root)
    paths = [p for p in diff.decode().split('\0') if p]
    if not any(p.startswith('Assets/_Project/Scripts/') and p.endswith('.cs') for p in paths):
        return []
    specs = [root / p for p in paths if p.startswith('docs/specs/BS-SPEC-') and p.endswith('.md')]
    if any(p.exists() and not check_spec(p) for p in specs):
        return []
    return ['Runtime code changed: include one scoped BS-SPEC with a technical boundary review. '
            'Historical specs need not be rewritten; see SPEC_TEMPLATE.md.']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--spec', type=Path, help='Review the current spec before implementation/commit.')
    parser.add_argument('--base', help='CI base commit used to require a boundary review for runtime changes.')
    args = parser.parse_args()
    root = args.root.resolve()
    errors = check_rules(root)
    if args.spec:
        errors += check_spec(root / args.spec)
    if args.base:
        try:
            errors += check_change_review(root, args.base)
        except subprocess.CalledProcessError:
            errors.append('Cannot inspect base commit; fetch history before running the review gate.')
    if errors:
        print('\n'.join(errors), file=sys.stderr)
        return 1
    print('Boundary checks passed (offline rules + requested spec review).')
    return 0


if __name__ == '__main__':
    sys.exit(main())
