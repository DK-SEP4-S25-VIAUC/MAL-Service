# tests/conftest.py
import os
import sys

# Setting project-roden (one dir up) on sys.path, so 'import src.…' works when running tests
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
if ROOT not in sys.path:
    sys.path.insert(0, ROOT)
