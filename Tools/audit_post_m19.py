"""Verify the repair against f6fb68c without reserializing functional documents."""
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
BASE = "f6fb68c"
SCENE = "Assets/_Project/Scenes/PrototypeRestaurant.unity"

def blob(path):
    return subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT).decode("utf-8").replace("\r\n", "\n")

def blocks(text):
    return {int(re.search(r"^--- !u!\d+ &(-?\d+)", b).group(1)): b
            for b in re.split(r"(?=^--- !u!)", text, flags=re.M)[1:]}

before = blocks(blob(SCENE))
after = blocks((ROOT / SCENE).read_text(encoding="utf-8"))
assert before.keys() == after.keys(), "Added or removed scene documents"
names = {i: re.search(r"  m_Name: (.*)", b).group(1) for i, b in before.items() if b.startswith("--- !u!1 &")}
transforms = {i: b for i, b in before.items() if b.startswith("--- !u!4 &")}
parents = {i: int(re.search(r"m_Father: \{fileID: (-?\d+)", b).group(1)) for i, b in transforms.items()}
objects = {int(re.search(r"m_GameObject: \{fileID: (-?\d+)", b).group(1)): i for i, b in transforms.items()}
def visual(t):
    if not t: return False
    g = int(re.search(r"m_GameObject: \{fileID: (-?\d+)", transforms[t]).group(1))
    return names[g] == "Visual_VP1BC" or visual(parents[t])
changes = []
physics = functional_transforms = 0
for i, old in before.items():
    new = after[i]
    if re.match(r"--- !u!(54|65|136|143) &", old):
        assert old == new; physics += 1
    if old.startswith("--- !u!4 &") and not visual(i):
        assert old == new; functional_transforms += 1
    if old == new: continue
    g = int(re.search(r"m_GameObject: \{fileID: (-?\d+)", old).group(1))
    assert visual(objects[g]), ("Changed nonvisual component", i, names[g])
    kind = re.search(r"--- !u!(\d+)", old).group(1)
    pattern = {"4": r"(?m)^  m_LocalPosition: .+$", "33": r"(?m)^  m_Mesh: .+$", "23": r"(?m)^  m_Enabled: .+$"}[kind]
    assert re.sub(pattern, "APPROVED", old) == re.sub(pattern, "APPROVED", new), ("Unexpected visual mutation", i)
    changes.append({"id": i, "object": names[g], "type": kind})
for b in after.values():
    for ref in re.findall(r"\{fileID: (-?\d+)\}", b):
        assert int(ref) == 0 or int(ref) in after, ("Missing scene reference", ref)
protected = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", BASE,
    "Assets/_Project/Materials", "Assets/_Project/Prefabs", "Assets/_Project/ScriptableObjects", "Assets/_Project/Scripts/Domain",
    "Packages", "ProjectSettings", "Assets/Settings", "Assets/_Project/Art/Restaurant"], cwd=ROOT).decode().splitlines()
for path in protected:
    assert (ROOT / path).read_bytes().replace(b"\r\n", b"\n") == subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT).replace(b"\r\n", b"\n"), path
metas = {}
for meta in (ROOT / "Assets").rglob("*.meta"):
    m = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(encoding="utf-8-sig"), re.M)
    if not m: continue
    assert m[1] not in metas, ("Duplicate GUID", meta, metas.get(m[1]))
    metas[m[1]] = meta
    assert meta.with_suffix("").exists(), ("Orphan meta", meta)
result = dict(base=BASE, scene_documents=len(before), functional_transforms_preserved=functional_transforms,
    physics_documents_preserved=physics, protected_files_identical=len(protected), visual_changes=changes,
    scene_sha256=hashlib.sha256((ROOT / SCENE).read_bytes()).hexdigest())
out = ROOT / "Build/PostM19/Audit.json"; out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps({k: len(v) if k == "visual_changes" else v for k, v in result.items()}, indent=2))
