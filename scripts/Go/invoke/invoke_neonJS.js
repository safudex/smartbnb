const Neon=require("@cityofzion/neon-js")
const fs = require('fs');
const axios = require('axios');

var ECO_WALLET = new Neon.wallet.Account("KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr");

function toBase58(beScriptHash) {
  if(beScriptHash.length==42) // remove '0x'
    beScriptHash = beScriptHash.slice(2);
  if(beScriptHash.length==40) // 20 bytes (40 char hexstring)
    return Neon.wallet.getAddressFromScriptHash(beScriptHash);
  else
    return "";
}

function createGasAndNeoIntent(to, neo, gas) {
    var intent;
    if (neo > 0 && gas > 0)
        intent = Neon.api.makeIntent({
            NEO: neo,
            GAS: gas
        }, to)

    if (neo == 0 && gas > 0)
        intent = Neon.api.makeIntent({
            GAS: gas
        }, to)

    if (neo > 0 && gas == 0)
        intent = Neon.api.makeIntent({
            NEO: neo
        }, to)
    return intent;
}

function revertHexString(hex) {
    return Neon.u.reverseHex(hex);
}

function signTXWithSingleSigner(signerAccount, constructTxPromise) {
    return signTxPromise = constructTxPromise.then(transaction => {

        transaction.addAttribute(32, revertHexString(signerAccount._scriptHash));

        if (transaction.inputs.length == 0)
            transaction.addRemark(Date.now().toString() + Neon.u.ab2hexstring(Neon.u.generateRandomArray(4)));

        const txHex = transaction.serialize(false);

        transaction.addWitness(Neon.tx.Witness.fromSignature(Neon.wallet.sign(txHex, signerAccount.privateKey), signerAccount.publicKey));

        return transaction;
    });
}

async function InvokeFromAccount(idToInvoke, mynetfee, mysysgasfee, neo, gas, contract_scripthash, contract_operation, nodeToCall, networkToCall, neonJSParams) {
    if (contract_scripthash == "" || !Neon.default.is.scriptHash(contract_scripthash)) {
        return "";
    }

	//setNeonApiProvider(networkToCall);
	NEON_API_PROVIDER = new Neon.api.neoCli.instance(nodeToCall);

    var intent = createGasAndNeoIntent(toBase58(contract_scripthash), neo, gas);

    var sb = Neon.default.create.scriptBuilder(); //new ScriptBuilder();
    // PUSH parameters BACKWARDS!!
    for (var i = neonJSParams.length - 1; i >= 0; i--)
        sb._emitParam(neonJSParams[i]);
    sb._emitAppCall(contract_scripthash, false); // tailCall = false
    var myscript = sb.str;

    var constructTx = NEON_API_PROVIDER.getBalance(ECO_WALLET._address).then(balance => {
        // Create invocation transaction with desired systemgas (param gas)
        let transaction = new Neon.tx.InvocationTransaction({
            gas: mysysgasfee
        });

        // Attach intents
        if (neo > 0)
            transaction.addIntent("NEO", neo, toBase58(contract_scripthash));
        if (gas > 0)
            transaction.addIntent("GAS", gas, toBase58(contract_scripthash));

        // addint invocation script
        transaction.script = myscript;

        // Attach extra network fee when calculating inputs and outputs
        transaction.calculate(balance, null, mynetfee);

        return transaction;
    });

    var invokeParams = transformInvokeParams(ECO_WALLET._address, mynetfee, mysysgasfee, neo, gas, neonJSParams, contract_scripthash);
    const signedTx = signTXWithSingleSigner(ECO_WALLET, constructTx);

    var txHash;
    return signedTx
        .then(transaction => {
            txHash = transaction.hash;
            const client = new Neon.rpc.RPCClient(nodeToCall);
            return client.sendRawTransaction(transaction.serialize(true));
        })
        .then(res => {
			return handleInvoke(res, txHash, invokeParams, contract_scripthash);
        })
        .catch(handleErrorInvoke);

}

function handleInvoke(res, txHash, invokeParams, contract_scripthash){
	fs.appendFileSync('./logs/invokes', JSON.stringify(res, txHash) + "\n");
	return res ? txHash: ""
}

function handleErrorInvoke(err){
	fs.appendFileSync('./logs/invokes', JSON.stringify(err)+"\n");
    console.log("err:::::::::::", err)
	return "";
}

function pushParams(neonJSParams, type, value, ) {
    if (type == 'String')
        neonJSParams.push(Neon.default.create.contractParam(type, value));
    else if (type == 'Address')
        neonJSParams.push(Neon.sc.ContractParam.byteArray(value, 'address'));
    else if (type == 'Hex')
        neonJSParams.push(Neon.default.create.contractParam('ByteArray', value));
    else if (type == 'DecFixed8')
        // Decimal fixed 8 seems to break at transition 92233720368.54775807 -> 92233720368.54775808
        neonJSParams.push(Neon.sc.ContractParam.byteArray(value, 'fixed8'))
	else if (type == 'Integer')
        neonJSParams.push(Neon.sc.ContractParam.integer(value));
    else if (type == 'Array')
        neonJSParams.push(Neon.default.create.contractParam(type, value));
}

function transformInvokeParams(myaddress, mynetfee, mysysgasfee, neo, gas, neonJSParams, contract_scripthash) {
    var invokeParams = {
        contract_scripthash: contract_scripthash,
        caller: myaddress,
        mynetfee: mynetfee,
        mysysgasfee: mysysgasfee,
        neo: neo,
        gas: gas,
        neonJSParams: neonJSParams,
        type: "invoke"
    }
    return invokeParams;
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}
var itry = 0
function randomInt(low, high) {
  return Math.floor(Math.random() * (high - low) + low)
}
var node = "https://node"+randomInt(1, 3)+".neocompiler.io"
async function invoke(scriptHash, invokeArr){
	fs.appendFileSync('./logs/invokes', "invoking: "+ " "+invokeArr[0].value+ " "+invokeArr[1].value[2].value+ " "+invokeArr[1].value[3].value)
    const txHash = await InvokeFromAccount(0, 0, 500, 0, 0, scriptHash, "", node, "SharedPrivateNet", invokeArr)
	fs.appendFileSync('./logs/invokes', "invoked: "+" "+ invokeArr[0].value+" "+ invokeArr[1].value[2].value+ " "+invokeArr[1].value[3].value)
    const data = { jsonrpc: "2.0", id: 5, method: "getapplicationlog", params: [txHash] }
    var res;
	do {
		console.log("testing status")
        res = await axios.post(node, data)
                .then((res) => {
					fs.appendFileSync('./logs/invokes', itry +" "+JSON.stringify(res.data)+"\n");
                    console.log(itry++, res.data)
                    console.log(res.data.result.executions[0].gas_consumed)
                    return res.data.result.executions[0].vmstate;
                }).catch((err) => {
					fs.appendFileSync('./logs/invokes', itry +" "+JSON.stringify(err)+"\n");
                    return ""
                });
        if (itry>100) res = true
        if (!res) sleep(1000)
    } while (!res)
    console.log(res)
	itry = 0
}
const cmdArgs = process.argv.slice(2)
async function SaveState(scriptHash){
		var neonJSParams = [];
		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', cmdArgs[1]);
		//2 byte[][] signatures
		var sigs = cmdArgs[2].split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', sigs);
		//3 bigint[] xs
		var xs = cmdArgs[3].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', xs);
		//4 bigint[] ys
		var ys = cmdArgs[4].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', ys);
		//5 byte[][] signablebytes
		var signableBytes = cmdArgs[5].split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', signableBytes);
		//6 byte[] blockhash
		var header = cmdArgs[6]
		pushParams(neonJSParams, 'Hex', header);
		//7 ulong[][] pres
		var pres = cmdArgs[7].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', pres);
		//8 ulong[][] preshash
		var presHash = cmdArgs[8].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', presHash);
		//9 bigint[] preshashmod
		var presHashMod = cmdArgs[9].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', presHashMod);
		//10 bigint[][] sB
		var sb = cmdArgs[10].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', sb);
		//11 bigint[][] ha
		var ha = cmdArgs[11].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', ha);
		invokeArr = []
		pushParams(invokeArr, 'String', "savestate");
		pushParams(invokeArr, 'Array', neonJSParams);
		await invoke(scriptHash, invokeArr)

		//12 bigint[][] Qs_sb
		//13 bigint[] ss_sb
		//14 bigint[][] Ps_sb
		var pointMulData=fs.readFileSync('pointmulsteps', 'utf-8').split("||");
		var ss_sb = []
		var Ps_sb=[]
		var Qs_sb=[]
		var pointMuls_sb = pointMulData[0].split(" ")
		pointMuls_sb.map(v => {
			var all = v.split(",")
			ss_sb = ss_sb.concat(all.slice(0, 32).map(Neon.sc.ContractParam.integer))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(Neon.sc.ContractParam.integer)
			Qs_sb = Qs_sb.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(Neon.sc.ContractParam.integer)
			Ps_sb = Ps_sb.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

		await savePointMuls(Ps_sb, 2, "Ps_sb", "multi")
		await savePointMuls(ss_sb, 2, "ss_sb", "simple")
		await savePointMuls(Qs_sb, 2, "Qs_sb", "multi")

		
		//Second invoke, state pointmul ha
		neonJSParams = []
		var ss_ha = []
		var Ps_ha=[]
		var Qs_ha=[]
		var pointMuls_ha = pointMulData[1].split(" ")
		pointMuls_ha.map(v => {
			var all = v.split(",")
			ss_ha = ss_ha.concat(all.slice(0, 32).map(Neon.sc.ContractParam.integer))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(Neon.sc.ContractParam.integer)
			Qs_ha = Qs_ha.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(Neon.sc.ContractParam.integer)
			Ps_ha = Ps_ha.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

		await savePointMuls(Ps_ha, 2, "Ps_ha", "multi")
		await savePointMuls(ss_ha, 2, "ss_ha", "simple")
		await savePointMuls(Qs_ha, 2, "Qs_ha", "multi")
}

async function savePointMuls(arr, nchks, id, type) {
		var j = arr.length/nchks
		for (var i=0; i<nchks; i++) {
				neonJSParams = []
				//0 byte[] calleraddr
				pushParams(neonJSParams, 'Address', ECO_WALLET._address);
				//1 byte[] txid
				pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
				pushParams(neonJSParams, 'String', type)
				pushParams(neonJSParams, 'String', id+i)
				console.log(id+i)
				pushParams(neonJSParams, 'Array', arr.slice(i*j, (i+1)*j))
				var invokeArr = []
				pushParams(invokeArr, 'String', "savestate_mul");
				pushParams(invokeArr, 'Array', neonJSParams);
				console.log("invokeing...", i)
				console.log(invokeArr[1].value[4].value[0])
				await invoke(scriptHash, invokeArr)
		}
}

function arrayTo2DArray1(list, howMany) {
  var result = []
  input = list.slice(0)
  while (input[0]) {
    result.push(input.splice(0, howMany))
  }
  return result
}

function Challenge0(scriptHash){
		var neonJSParams = [];

		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
		//2 int signature id
		pushParams(neonJSParams, 'Integer', 1);
		var invokeArr = []
		pushParams(invokeArr, 'String', "challenge 0");
		pushParams(invokeArr, 'Array', neonJSParams);

		invoke(scriptHash, invokeArr)
}

function Challenge1(scriptHash){
		var neonJSParams = [];

		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
		//2 int signature id
		pushParams(neonJSParams, 'Integer', 0);
		var invokeArr = []
		pushParams(invokeArr, 'String', "challenge 1");
		pushParams(invokeArr, 'Array', neonJSParams);

		invoke(scriptHash, invokeArr)
}

function Challenge2(scriptHash){
		var neonJSParams = [];

		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
		//2 int signature id
		pushParams(neonJSParams, 'Integer', 0);
		var invokeArr = []
		pushParams(invokeArr, 'String', "challenge 2");
		pushParams(invokeArr, 'Array', neonJSParams);

		invoke(scriptHash, invokeArr)
}

function Challenge3(scriptHash){
		var neonJSParams = [];

		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
		//2 int signature id
		pushParams(neonJSParams, 'Integer', 0);
		var invokeArr = []
		pushParams(invokeArr, 'String', "challenge 3");
		pushParams(invokeArr, 'Array', neonJSParams);

		invoke(scriptHash, invokeArr)
}

function Challenge4(scriptHash){
		var neonJSParams = [];

		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
		//2 int signature id
		pushParams(neonJSParams, 'Integer', 0);
		var invokeArr = []
		pushParams(invokeArr, 'String', "challenge 4");
		pushParams(invokeArr, 'Array', neonJSParams);

		invoke(scriptHash, invokeArr)
}

function Challenge5(scriptHash){
        var neonJSParams = [];

        //0 byte[] calleraddr
        pushParams(neonJSParams, 'Address', ECO_WALLET._address);
        //1 byte[] txid
        pushParams(neonJSParams, 'Hex', "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6");
        //2 int signature id
        pushParams(neonJSParams, 'Integer', 0);
        //3 int step num
        pushParams(neonJSParams, 'Integer', 0)
        pushParams(neonJSParams, 'String', "sb");

        var invokeArr = []
        pushParams(invokeArr, 'String', "challenge 5");
        pushParams(invokeArr, 'Array', neonJSParams);
        invoke(scriptHash, invokeArr)
}

var scriptHash = "8aea8eac3b09b55aa8bbb9f47acd15821002972d"
SaveState(scriptHash)
//Challenge5(scriptHash)
//Challenge3(scriptHash)
