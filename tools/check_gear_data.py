"""Validate the proposed gear cards, references and saved-instance example; not the native content loader."""
from copy import deepcopy
import json
from pathlib import Path
from math import isclose
from jsonschema import Draft202012Validator
from jsonschema.exceptions import ValidationError

ROOT = Path(__file__).resolve().parents[1]
SCHEMA = json.loads((ROOT / 'schemas/gear-definition.schema.json').read_text())
Draft202012Validator.check_schema(SCHEMA)
VALIDATOR = Draft202012Validator(SCHEMA)


def validate_card(card):
    VALIDATOR.validate(card)
    def unique(records, name):
        ids = [r['id'] for r in records]
        if len(ids) != len(set(ids)):
            raise ValueError('Duplicate ' + name)
        return set(ids)
    forms = unique(card['forms'], 'form')
    if card['defaultForm'] not in forms:
        raise ValueError('Missing default form')
    if not isclose(sum(m['massFraction'] for m in card['materials']), 1, abs_tol=1e-8):
        raise ValueError('Material fractions must sum to one')
    if card['condition']['default'] > card['condition']['maximum']:
        raise ValueError('Condition exceeds maximum')
    compartments = card.get('compartments', [])
    regions = unique(compartments, 'compartment')
    closures = unique(card.get('closures', []), 'closure')
    surfaces = unique(card.get('attachmentSurfaces', []), 'attachment surface')
    unique(card.get('functions', []), 'function')
    unique(card['appearance'].get('segments', []), 'segment')
    if sum(c.get('defaultDrop', False) for c in compartments) > 1:
        raise ValueError('Multiple default drop compartments')
    def check_mask(grid, key):
        if any(v >= grid['columns'] * grid['rows'] for v in grid.get(key, [])):
            raise ValueError('Mask outside grid')
    for form in card['forms']:
        check_mask(form['squareShape'], 'usableCells')
        if any(v <= 0 for v in form['envelopeMm']):
            raise ValueError('Form envelope must be positive')
        shape = form['squareShape']
        if (shape['columns'] * form['referenceCellMm'] < form['envelopeMm'][0]
                or shape['rows'] * form['referenceCellMm'] < form['envelopeMm'][1]):
            raise ValueError('Reference shape smaller than physical envelope')
    for c in compartments:
        if c['kind'] == 'storage':
            check_mask(c['grid'], 'usableCells')
    controlled = set()
    for closure in card.get('closures', []):
        for target in closure['targets']:
            if target not in regions and not (target == 'garment-front' and 'wear' in card):
                raise ValueError('Closure targets unknown region')
            if target in controlled:
                raise ValueError('Multiple closures require a future explicit gate expression')
            controlled.add(target)
            c = next((c for c in compartments if c['id'] == target), None)
            if c and c['defaultOpen'] != closure['defaultOpen']:
                raise ValueError('Closure and compartment defaults disagree')
    for segment in card['appearance'].get('segments', []):
        for key, allowed in [('compartment', regions), ('closure', closures), ('attachmentSurface', surfaces)]:
            if key in segment and segment[key] not in allowed:
                raise ValueError('Segment has unknown ' + key)
        if not any(k in segment for k in ['compartment', 'closure', 'attachmentSurface', 'bodySurface']):
            raise ValueError('Segment has no interaction target')


def validate_instance(instance, cards):
    Draft202012Validator({'$ref': '#/$defs/instance', '$defs': SCHEMA['$defs']}).validate(instance)
    card = cards[instance['definition']]
    if instance['form'] not in {f['id'] for f in card['forms']}:
        raise ValueError('Unknown instance form')
    if instance['condition'] > card['condition']['maximum']:
        raise ValueError('Instance condition out of range')
    if set(instance.get('compartments', {})) != {c['id'] for c in card.get('compartments', [])}:
        raise ValueError('Instance compartment identity mismatch')
    if set(instance.get('closures', {})) != {c['id'] for c in card.get('closures', [])}:
        raise ValueError('Instance closure identity mismatch')
    orientation = instance['parent'].get('orientation', 0)
    allowed = [0, 60, 120, 180, 240, 300] if instance['parent']['relation'] in ['attached', 'worn'] else [0, 90]
    if orientation not in allowed:
        raise ValueError('Wrong orientation domain')


def main():
    cards = {}
    for path in sorted((ROOT / 'examples/gear').glob('*.json')):
        if path.name.endswith('.instance.json'):
            continue
        card = json.loads(path.read_text())
        validate_card(card)
        if card['id'] in cards:
            raise ValueError('Duplicate catalog ID')
        cards[card['id']] = card
    instance = json.loads((ROOT / 'examples/gear/backpack.instance.json').read_text())
    validate_instance(instance, cards)
    rejected = 0
    def reject(mutator):
        nonlocal rejected
        bad = deepcopy(cards['example:backpack'])
        mutator(bad)
        try:
            validate_card(bad)
        except (ValueError, ValidationError):
            rejected += 1
            return
        raise AssertionError('Invalid author card accepted')
    reject(lambda c: c['forms'][0].update(orientations=[0, 60]))
    reject(lambda c: c['compartments'].append(deepcopy(c['compartments'][0])))
    reject(lambda c: c['appearance']['segments'][0].update(compartment='missing'))
    reject(lambda c: c['closures'][0].update(targets=['missing']))
    reject(lambda c: c['compartments'][0]['grid'].update(usableCells=[9999]))
    reject(lambda c: c['materials'][0].update(massFraction=.5))
    reject(lambda c: c['compartments'][1].update(defaultDrop=True))
    reject(lambda c: c['compartments'][0].update(defaultOpen=True))
    reject(lambda c: c.update(unregisteredMagic=True))
    reject(lambda c: c.update(extensions={'unscoped': {}}))
    reject(lambda c: c['forms'][0].update(envelopeMm=[9999, 100, 40]))
    reject(lambda c: c['wear']['fit'].update(bulk=0))
    reject(lambda c: c['wear']['fit'].update(fitsOver=['core:light-textile', 'core:light-textile']))
    reject(lambda c: c['wear']['fit'].update(family='bad token'))
    reject(lambda c: c['wear'].pop('fit'))
    print(f'GEAR_STANDARD_CHECKS_PASS: {len(cards)} definitions, 1 instance, {rejected} invalid author cards rejected.')


if __name__ == '__main__':
    main()
