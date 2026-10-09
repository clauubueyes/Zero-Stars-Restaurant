"""Compare the incremental presentation fix with b023fe8 and the saved current scene."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = 'b023fe8'


def blocks(text):
    return {int(re.match(r'--- !u!\d+ &(-?\d+)', b)[1]): b.rstrip()
            for b in re.split(r'(?=^--- !u!)', text.replace('\r\n', '\n'), flags=re.M)[1:]}


def audit(path, old):
    before = blocks(old.decode('utf-8-sig'))
    after = blocks((ROOT/path).read_text(encoding='utf-8-sig'))
    result = dict(transforms_preserved=0, physics_preserved=0, presentation_profiles=0, added_support_metadata=0)
    for key, original in before.items():
        assert key in after, ('Removed original document', path, key)
        current = after[key]
        if 'ZeroStarRestaurant.Presentation.BurgerIngredientVisual' in original:
            assert re.sub(r'(?m)^  _(heightScale|supportingHeightScale|diameterScale):[^\n]*\n?', '', current).rstrip() == original
            result['presentation_profiles'] += 1
        elif original.startswith('--- !u!1 &'):
            pattern = r'(?m)^  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)*'
            assert re.sub(pattern, 'COMPONENTS\n', original) == re.sub(pattern, 'COMPONENTS\n', current)
            old_ids = re.findall(r'- component: \{fileID: (-?\d+)\}', original)
            new_ids = re.findall(r'- component: \{fileID: (-?\d+)\}', current)
            assert new_ids[:len(old_ids)] == old_ids
            for component in new_ids[len(old_ids):]:
                assert int(component) not in before and 'ZeroStarRestaurant.Presentation.FoodVisualSupportSurface' in after[int(component)]
        else:
            assert original == current, ('Changed protected scene/prefab document', path, key)
            result['transforms_preserved'] += original.startswith('--- !u!4 &')
            result['physics_preserved'] += bool(re.match(r'--- !u!(54|65|136|143) &', original))
    for key in after.keys() - before.keys():
        assert after[key].startswith('--- !u!114 &') and 'ZeroStarRestaurant.Presentation.FoodVisualSupportSurface' in after[key]
        result['added_support_metadata'] += 1
    for doc in after.values():
        for reference in re.finditer(r'\{fileID: (-?\d+)\}', doc):
            assert int(reference[1]) == 0 or int(reference[1]) in after, (path, 'Unresolved local reference', reference[1])
    return result


tree = subprocess.check_output(['git', 'ls-tree', '-r', BASE, 'Assets', 'Packages', 'ProjectSettings'], cwd=ROOT).decode().splitlines()
entries = [(line.split('\t', 1)[1], line.split()[2]) for line in tree]
payload = subprocess.check_output(['git', 'cat-file', '--batch'], input=('\n'.join(sha for _, sha in entries)+'\n').encode(), cwd=ROOT)
offset = 0
source = {}
for path, sha in entries:
    end = payload.index(b'\n', offset)
    header = payload[offset:end].split()
    assert header[0].decode() == sha and header[1] == b'blob'
    length = int(header[2]); source[path] = payload[end+1:end+1+length]; offset = end+length+2
targets = ['Assets/_Project/Scenes/PrototypeRestaurant.unity', *[f'Assets/_Project/Prefabs/Food/{p}.prefab' for p in ('Bun', 'RawBeefPatty', 'Cheese')]]
saved = ROOT/'Build/FoodVisualPlacement/PrototypeRestaurant.before.unity'
result = {path: audit(path, saved.read_bytes() if path.endswith('.unity') else source[path]) for path in targets}
allowed = set(targets + ['Assets/_Project/Scripts/Runtime/Presentation/BurgerIngredientVisual.cs', 'Assets/_Project/Scripts/Runtime/Interaction/InteractionDetector.cs', 'Assets/_Project/Editor/VisualPolishBuilder.cs'])
protected = 0
for path, original in source.items():
    if path in allowed:
        continue
    current = (ROOT/path).read_bytes()
    if Path(path).suffix not in ('.png', '.jpg', '.wav', '.ogg', '.fbx'):
        original = original.replace(b'\r\n', b'\n'); current = current.replace(b'\r\n', b'\n')
    assert original == current, ('Changed protected asset/code/config', path)
    protected += 1
guids = set()
for meta in (ROOT/'Assets').rglob('*.meta'):
    guid = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(encoding='utf-8-sig'), re.M)
    assert guid is not None, ('Invalid GUID', meta)
    if guid:
        assert guid[1] not in guids and meta.with_suffix('').exists(), ('Duplicate/orphan meta', meta)
        guids.add(guid[1])
result['protected_files_identical'] = protected
(ROOT/'Build/FoodVisualPlacement/Audit.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result, indent=2))
