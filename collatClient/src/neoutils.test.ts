import hex2int from './neoutils';

test('hex2int conevrts neovm bytearrays to ints properly', () => {
  expect(hex2int({
    type: 'Integer',
    value: '1000000000',
  })).toBe(1000000000);
  expect(hex2int({
    type: 'ByteArray',
    value: '00e1f505',
  })).toBe(100000000);
});
