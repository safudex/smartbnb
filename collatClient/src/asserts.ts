function assertDefined<T>(val: T, varName?:string): asserts val is NonNullable<T> {
  if (val === undefined || val === null) {
    throw new Error(
      `Expected ${varName} to be defined, but received ${val}`,
    );
  }
}

export default assertDefined;
