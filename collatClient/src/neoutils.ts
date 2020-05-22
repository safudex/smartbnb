function hex2int(hex: string): number {
  const reverseHex = (rhex: string) => rhex.match(/.{2}/g)?.reverse()?.join('') ?? '0';
  return parseInt(reverseHex(hex), 16);
}

export default hex2int;
