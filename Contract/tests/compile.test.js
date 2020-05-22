const fs = require("fs");
contractCode = fs.readFileSync("../smartBNB/Contract.cs");

jest.setTimeout(40000);
describe('Compilation', () => {
  test('compiles on 2.9.3', async () => {
    await page.goto('https://neocompiler.io/#!/ecolab/compilers');
    await delay(10000);
    const compilerSelection = await page.$("#compilers_versions-selection-box")
    await compilerSelection.select('docker-mono-neo-compiler:2.9.3');
    await page.evaluate(code => {
      // this will be executed within the page's context
      window.aceEditor.getSession().setValue(code);
    }, contractCode.toString());
    await testCompilation();
  });
});

async function testCompilation(){
    await expect(page).toClick("#compilebtn");
    await delay(10000);
    const output = await page.$eval("#codeavm", n => n.value);
    expect(output).not.toBe("");
}

function delay(time) {
   return new Promise(function(resolve) {
       setTimeout(resolve, time)
   });
}
