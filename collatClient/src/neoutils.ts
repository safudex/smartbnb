function hex2int(param: {type:string, value:string}): number {
  if (param.type === 'Integer') {
    return parseInt(param.value, 10);
  }
  const reverseHex = (rhex: string) => rhex.match(/.{2}/g)?.reverse()?.join('') ?? '0';
  return parseInt(reverseHex(param.value), 16);
}

export default hex2int;
