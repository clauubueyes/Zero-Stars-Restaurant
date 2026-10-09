"""Verify original Unity documents against the saved VP1A scene/prefabs, without Unity dependencies."""
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = "db39056"


def blocks(text):
    return {int(re.match(r"--- !u!\d+ &(-?\d+)", b).group(1)): b.rstrip()
            for b in re.split(r"(?=^--- !u!)", text.replace("\r\n", "\n"), flags=re.M)[1:]}


def baseline(path):
    return subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT).decode("utf-8-sig")


def verify(path):
    current = (ROOT / path).read_text(encoding="utf-8-sig")
    before_path = ROOT / "Build/VP1BC/PrototypeRestaurant.before.unity"
    source = before_path.read_text(encoding="utf-8-sig") if path.endswith(".unity") and before_path.exists() else baseline(path)
    before, after = blocks(source), blocks(current)
    count = {"original_documents": len(before), "added_documents": len(after) - len(before),
             "original_transforms_preserved": 0, "original_physics_preserved": 0, "changed_original_renderers": 0}
    for file_id, old in before.items():
        assert file_id in after, (path, "Removed original", file_id)
        new = after[file_id]
        if old.startswith("--- !u!4 &"):
            pattern = r"(?m)^  m_Children:(?: \[\])?\n(?:  - \{fileID: [^\r\n]*\}\n)*"
            assert re.sub(pattern, "CHILDREN\n", old) == re.sub(pattern, "CHILDREN\n", new), (path, "Changed transform", file_id)
            old_list = re.search(pattern, old).group(0); new_list = re.search(pattern, new).group(0)
            assert all(c in new_list for c in re.findall(r"\{fileID: -?\d+\}", old_list))
            old_refs = re.findall(r"\{fileID: -?\d+\}", old_list)
            assert re.findall(r"\{fileID: -?\d+\}", new_list)[:len(old_refs)] == old_refs, "Original child order changed"
            count["original_transforms_preserved"] += 1
        elif old.startswith("--- !u!23 &"):
            pattern = r"(?m)^  m_Enabled: \d+$"
            assert re.sub(pattern, "ENABLED", old) == re.sub(pattern, "ENABLED", new), (path, "Changed original material/renderer", file_id)
            count["changed_original_renderers"] += old != new
        elif old.startswith("--- !u!1660057539 &"):
            assert all(c in new for c in re.findall(r"\{fileID: -?\d+\}", old))
        else:
            assert old == new, (path, "Changed protected gameplay/config", file_id, old[:160], new[:160])
            if re.match(r"--- !u!(54|65|136|143) &", old): count["original_physics_preserved"] += 1
    added = [v for k, v in after.items() if k not in before]
    assert not any(re.match(r"--- !u!(54|65|136|143|108) &", b) for b in added), "New art physics or light"
    for b in after.values():
        for match in re.finditer(r"\{fileID: (-?\d+)\}", b):
            assert int(match.group(1)) == 0 or int(match.group(1)) in after, (path, "Unresolved local ref", match.group(1))
    count["added_renderers"] = sum(b.startswith("--- !u!23 &") for b in added)
    count["sha256"] = hashlib.sha256(current.encode()).hexdigest()
    return count


def main():
    files = ["Assets/_Project/Scenes/PrototypeRestaurant.unity", *[f"Assets/_Project/Prefabs/{p}.prefab"
             for p in ("Food/Bun", "Food/RawBeefPatty", "Food/Cheese", "Plate")]]
    results = {p: verify(p) for p in files}
    protected = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", BASE, "Assets/_Project/Materials", "Packages", "ProjectSettings", "Assets/Settings"], cwd=ROOT).decode().splitlines()
    for path in protected:
        # Git's Windows checkout can use CRLF while its blob uses LF. Compare serialized content.
        assert (ROOT / path).read_bytes().replace(b"\r\n", b"\n") == subprocess.check_output(["git", "show", f"{BASE}:{path}"], cwd=ROOT).replace(b"\r\n", b"\n"), ("Changed protected file", path)
    results["protected_files_identical"] = len(protected)
    meta_guids = {}
    for meta in (ROOT / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(encoding="utf-8-sig"), re.M)
        if not match: continue
        guid = match.group(1); assert guid not in meta_guids, ("Duplicate GUID", meta, meta_guids.get(guid))
        meta_guids[guid] = str(meta.relative_to(ROOT))
        assert meta.with_suffix("").exists(), ("Orphan meta", meta)
    for item in (ROOT / "Assets/_Project/Art").rglob("*"):
        if not item.name.endswith(".meta"): assert Path(str(item) + ".meta").exists(), ("Missing meta", item)
    results["art_assets"] = len(list((ROOT / "Assets/_Project/Art/Restaurant").rglob("*.asset")))
    results["art_materials"] = len(list((ROOT / "Assets/_Project/Art/Restaurant").rglob("*.mat")))
    results["art_textures"] = len(list((ROOT / "Assets/_Project/Art/Restaurant").rglob("*.png")))
    output = ROOT / "Build/VP1BC/Audit.json"; output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
