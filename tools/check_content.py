#!/usr/bin/env python3
"""Repository content checks run in CI.

1. Source code and XAML under src/ contain no Turkish letters (names and UI text are English).
2. No tracked file contains a banned word. The words (names of real people, personal and institutional
   identifiers removed during anonymisation) are stored only as SHA-256 hashes in
   tools/banned_word_hashes.txt, so the list itself does not reveal them.
3. No tracked file is larger than 1 MB.

Usage: python3 tools/check_content.py   (run from the repository root; exits 1 on any finding)
"""
import hashlib
import pathlib
import re
import subprocess
import sys
import unicodedata

ROOT = pathlib.Path(__file__).resolve().parent.parent
HASH_FILE = ROOT / "tools" / "banned_word_hashes.txt"
MAX_BYTES = 1024 * 1024
TURKISH_LETTERS = re.compile("[çğıöşüÇĞİÖŞÜ]")
WORD = re.compile(r"[^\W\d_]+", re.UNICODE)
CODE_SUFFIXES = {".cs", ".xaml"}
BINARY_SUFFIXES = {".png", ".jpg", ".jpeg", ".ico", ".gif", ".db", ".xlsx", ".zip"}


def fold(word: str) -> str:
    """Lower-cases and strips accents so 'ÇAĞLA', 'Çağla' and 'cagla' compare equal."""
    word = word.replace("ı", "i").replace("İ", "I")
    decomposed = unicodedata.normalize("NFKD", word)
    return "".join(ch for ch in decomposed if not unicodedata.combining(ch)).lower()


def word_hash(word: str) -> str:
    return hashlib.sha256(fold(word).encode("utf-8")).hexdigest()


def tracked_files() -> list[pathlib.Path]:
    output = subprocess.run(["git", "ls-files", "-z"], cwd=ROOT, check=True, capture_output=True).stdout
    return [ROOT / name for name in output.decode("utf-8").split("\0") if name]


def main() -> int:
    banned = {line.strip() for line in HASH_FILE.read_text().splitlines() if line.strip() and not line.startswith("#")}
    findings: list[str] = []

    for path in tracked_files():
        if not path.is_file():
            continue
        relative = path.relative_to(ROOT).as_posix()

        if path.stat().st_size > MAX_BYTES:
            findings.append(f"{relative}: larger than 1 MB")

        if path.suffix.lower() in BINARY_SUFFIXES or path == HASH_FILE:
            continue
        text = path.read_text(encoding="utf-8", errors="replace")

        if relative.startswith("src/") and path.suffix.lower() in CODE_SUFFIXES:
            for number, line in enumerate(text.splitlines(), 1):
                if TURKISH_LETTERS.search(line):
                    findings.append(f"{relative}:{number}: Turkish letter in source code")

        for number, line in enumerate(text.splitlines(), 1):
            for word in WORD.findall(line):
                if word_hash(word) in banned:
                    findings.append(f"{relative}:{number}: banned word")

    for finding in findings:
        print(finding)
    print(f"Content checks: {len(findings)} finding(s) in {len(tracked_files())} tracked files.")
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
