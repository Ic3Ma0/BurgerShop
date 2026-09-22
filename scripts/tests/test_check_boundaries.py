import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

path = Path(__file__).resolve().parents[1] / 'check_boundaries.py'
spec = importlib.util.spec_from_file_location('boundaries', path)
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)


class BoundaryGuardTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.rules = self.root / guard.RULES
        self.rules.mkdir(parents=True)
        self.definition = self.rules / (guard.ASSEMBLY + '.asmdef')
        self.definition.write_text(json.dumps(dict(name=guard.ASSEMBLY, references=[],
            overrideReferences=True, precompiledReferences=[], noEngineReferences=True)))
        self.code = self.rules / 'Rule.cs'
        self.code.write_text('using System; namespace BurgerShop.Economy { class Rule {} }')

    def test_clean_boundary_passes(self):
        self.assertEqual(guard.check_rules(self.root), [])

    def test_scene_io_and_ambient_clock_are_rejected(self):
        for source in ['using UnityEngine;', 'class A { UnityEngine.Component root; }',
                       'using BurgerShop.UI;', 'using System.IO;', 'System.DateTime.UtcNow;',
                       'using D = System.DateTime; D.UtcNow;', 'using static System.DateTime; UtcNow;',
                       'System.Environment.GetEnvironmentVariable("X");']:
            with self.subTest(source=source):
                self.code.write_text(source)
                self.assertTrue(guard.check_rules(self.root))

    def test_assembly_escape_is_rejected(self):
        original = json.loads(self.definition.read_text())
        for key, value in [('references', ['BurgerShop.Runtime']), ('noEngineReferences', False),
                           ('overrideReferences', False), ('precompiledReferences', ['plugin.dll'])]:
            with self.subTest(key=key):
                self.definition.write_text(json.dumps(dict(original, **{key: value})))
                self.assertTrue(guard.check_rules(self.root))
        self.definition.write_text(json.dumps(original))
        (self.rules / 'escape.asmref').write_text('{}')
        self.assertTrue(guard.check_rules(self.root))

    def test_comments_and_literals_are_not_dependencies(self):
        self.code.write_text('// Do not use UnityEngine or System.IO\n'
                             '/* DateTime.UtcNow */ class A { const string S = "UnityEngine"; }')
        self.assertEqual(guard.check_rules(self.root), [])

    def test_review_requires_state_and_dependency_decisions(self):
        review = self.root / 'spec.md'
        review.write_text('# Feature only\n')
        self.assertTrue(guard.check_spec(review))
        review.write_text(guard.REVIEW_SECTION + '\n' + '\n'.join(guard.REVIEW_ITEMS))
        self.assertEqual(guard.check_spec(review), [])

    def test_ci_diff_requires_review_for_runtime_changes(self):
        def git(*args):
            return subprocess.check_output(['git', *args], cwd=self.root, stderr=subprocess.DEVNULL).decode().strip()
        git('init')
        git('config', 'user.name', 'Boundary test')
        git('config', 'user.email', 'boundary@example.invalid')
        git('config', 'commit.gpgsign', 'false')
        git('add', '.')
        git('commit', '-m', 'fixture')
        base = git('rev-parse', 'HEAD')
        self.code.write_text(self.code.read_text() + '\n// scoped change\n')
        git('add', '.')
        git('commit', '-m', 'runtime without review')
        self.assertTrue(guard.check_change_review(self.root, base))
        review = self.root / 'docs/specs/BS-SPEC-TEST.md'
        review.parent.mkdir(parents=True)
        review.write_text(guard.REVIEW_SECTION + '\n' + '\n'.join(guard.REVIEW_ITEMS))
        git('add', '.')
        git('commit', '-m', 'scoped review')
        self.assertEqual(guard.check_change_review(self.root, base), [])


if __name__ == '__main__':
    unittest.main()
