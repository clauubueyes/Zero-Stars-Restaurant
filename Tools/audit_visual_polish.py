"""Audit the polish against the user's saved VP1B/C scene and e4fb2fd assets."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = 'e4fb2fd'


def blocks(text):
    return {int(re.match(r'--- !u!\d+ &(-?\d+)', b)[1]): b.rstrip()
            for b in re.split(r'(?=^--- !u!)', text.replace('\r\n', '\n'), flags=re.M)[1:]}


def baseline(path):
    saved = ROOT / 'Build/VisualPolish/PrototypeRestaurant.before.unity'
    return saved.read_text(encoding='utf-8-sig') if path.endswith('.unity') else subprocess.check_output(['git', 'show', f'{BASE}:{path}'], cwd=ROOT).decode('utf-8-sig')


def audit(path):
    old = blocks(baseline(path)); new = blocks((ROOT / path).read_text(encoding='utf-8-sig'))
    stains = {k for k, b in old.items() if b.startswith('--- !u!1 &') and re.search(r'^  m_Name: Residue \d+$', b, re.M)}
    counts = dict(original_documents=len(old), added_documents=len(new)-len(old), original_transforms_preserved=0, original_physics_preserved=0, dirt_renderers_updated=0)
    for key, before in old.items():
        assert key in new, ('Removed original', path, key)
        after = new[key]
        if before.startswith('--- !u!1 &'):
            pattern = r'(?m)^  m_Component:\n(?:  - component: \{fileID: -?\d+\}\n)*'
            assert re.sub(pattern, 'COMPONENTS\n', before) == re.sub(pattern, 'COMPONENTS\n', after)
            previous = re.findall(r'  - component: \{fileID: (-?\d+)\}', before)
            current = re.findall(r'  - component: \{fileID: (-?\d+)\}', after)
            assert current[:len(previous)] == previous
            assert all(int(k) not in old for k in current[len(previous):])
        elif before.startswith('--- !u!114 &') and 'ZeroStarRestaurant.Hygiene.DirtSurfaceView' in before:
            assert re.sub(r'(?m)^  _(organicOverlay|visualSeed):[^\n]*\n?', '', after).rstrip() == before
        elif re.match(r'--- !u!(23|33) &', before) and int(re.search(r'm_GameObject: \{fileID: (-?\d+)\}', before)[1]) in stains:
            for pattern in (r'(?m)^  m_Mesh:.*$', r'(?m)^  m_CastShadows:.*$', r'(?m)^  m_ReceiveShadows:.*$', r'(?m)^  m_Materials:\n(?:  - \{fileID: [^\r\n]*\}\n)*'):
                before = re.sub(pattern, 'VISUAL\n', before); after = re.sub(pattern, 'VISUAL\n', after)
            assert before == after, (path, 'Changed other residue fields', key)
            counts['dirt_renderers_updated'] += new[key].startswith('--- !u!23 &')
        else:
            assert before == after, (path, 'Changed protected document', key, before[:160])
            counts['original_transforms_preserved'] += before.startswith('--- !u!4 &')
            counts['original_physics_preserved'] += bool(re.match(r'--- !u!(54|65|136|143) &', before))
    for key in new.keys() - old.keys():
        assert new[key].startswith('--- !u!114 &') and 'ZeroStarRestaurant.Presentation.BurgerIngredientVisual' in new[key]
    for b in new.values():
        for m in re.finditer(r'\{fileID: (-?\d+)\}', b):
            assert int(m[1]) == 0 or int(m[1]) in new, ('Unresolved local reference', path, m[1])
    return counts


def main():
    paths = ['Assets/_Project/Scenes/PrototypeRestaurant.unity', *[f'Assets/_Project/Prefabs/Food/{p}.prefab' for p in ('Bun', 'RawBeefPatty', 'Cheese')]]
    result = {p: audit(p) for p in paths}
    changed = set(paths + ['Assets/_Project/Scripts/Runtime/Hygiene/DirtSurfaceView.cs', 'Assets/_Project/Tests/EditMode/M1SceneTests.cs', 'Assets/_Project/Tests/EditMode/VP1BCArtSceneTests.cs'])
    protected = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', BASE, 'Assets', 'Packages', 'ProjectSettings'], cwd=ROOT).decode().splitlines()
    for path in protected:
        if path not in changed:
            assert (ROOT/path).read_bytes().replace(b'\r\n', b'\n') == subprocess.check_output(['git', 'show', f'{BASE}:{path}'], cwd=ROOT).replace(b'\r\n', b'\n'), ('Changed protected asset/code/config', path)
    result['protected_files_identical'] = len(protected) - len(changed)
    guids = set()
    for meta in (ROOT/'Assets').rglob('*.meta'):
        m = re.search(r'^guid: ([a-f0-9]{32})$', meta.read_text(encoding='utf-8-sig'), re.M)
        if m:
            assert m[1] not in guids, ('Duplicate GUID', meta); guids.add(m[1])
            assert meta.with_suffix('').exists(), ('Orphan meta', meta)
    for item in (ROOT/'Assets/_Project/Art/VisualPolish').rglob('*'):
        if not item.name.endswith('.meta'): assert Path(str(item)+'.meta').exists()
    output = ROOT/'Build/VisualPolish/Audit.json'
    output.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
