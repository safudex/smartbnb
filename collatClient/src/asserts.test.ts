import assertDefined from './asserts';

test('throws when undefined or null', () => {
  expect(() => assertDefined(null)).toThrow();
  expect(() => assertDefined(undefined)).toThrow();
});

test("doesn't throw when defined", () => {
  assertDefined('smth');
});

test('error message works properly', () => {
  expect(() => assertDefined(undefined, 'var')).toThrow('Expected var to be defined, but received undefined');
});
