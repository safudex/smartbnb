const fs = require('fs');
const axios = require('axios');
const Neon = require("@cityofzion/neon-js");
var ECO_WALLET = new Neon.wallet.Account("KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr");
var scriptHash = "d7fccdb9501bdd71db00581df8764dd92965ee2c" //const
const cmdArgs = process.argv.slice(2)
const node = "https://node"+randomInt(1, 3)+".neocompiler.io"
const api = new Neon.api.neoCli.instance(node);
const txHash = cmdArgs[1]

async function getTxResult(txid, leftAttemps){
    let txres = await axios.post(node, { jsonrpc: "2.0", id: 5, method: "getapplicationlog", params: [txid] })
    if (txres.data.error && leftAttemps > 0 ) {
        console.log(leftAttemps, txid, txres.data.error)
        sleep(1000)
        txres = await getTxResult(txid, leftAttemps-1)
    }
    else if (leftAttemps < 0) {
        console.log(leftAttemps, txid, txres.data.error)
        return null
    }
    
    return txres
}

async function invokeOperation(operation, args, gas, fees){
    console.log(args.length)

    let txr
    try {
        const response = await invoke(operation, args, gas, fees)
        txr = (await getTxResult(response.txid, 2*60)).data.result//.executions[0]
        if (!txr) {
            console.log("Attemps exhausted")
            //retry
        }
		else
	        console.log(txr.executions[0])//return txr
    } catch (error) {
        console.log(error.message)
    }
}

async function invoke(operation, args, gas, fees){
    return Neon.default.doInvoke({
        api, // The API Provider that we rely on for balance and rpc information
        account: ECO_WALLET, // The sending Account
        gas, // Additional GAS for invocation.
        fees,
        script: Neon.default.create.script({ scriptHash, operation, args })
    }).then(res => {return res.response})
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

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function randomInt(low, high) {
  return Math.floor(Math.random() * (high - low) + low)
}

async function SaveState(){
		var neonJSParams = [];
		//1 byte[] porting
		pushParams(neonJSParams, 'Hex', portingContractID)
		pushParams(neonJSParams, 'String', "GENERAL")
		
		var pres = cmdArgs[6].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
        pushParams(neonJSParams, 'Array', pres);
        
		//7 ulong[][] preshash
		var presHash = cmdArgs[7].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', presHash);
        
		//9 byte[] txproof
		pushParams(neonJSParams, 'Hex', cmdArgs[9]);
		//10 byte[] blockHeader
		pushParams(neonJSParams, 'Hex', cmdArgs[10]);
		//11 byte[] txbytes
		var txbytes = cmdArgs[11].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', txbytes);

		await invokeOperation("savestate", neonJSParams, 50, 10)

	    neonJSParams = []	
		//1 byte[] porting
		pushParams(neonJSParams, 'Hex', portingContractID)
        pushParams(neonJSParams, 'String', "PM")
        
		var signableBytes = cmdArgs[5].split(",").map(v=>v.match(/.{1,2}/g).map(v=>Neon.sc.ContractParam.integer(parseInt(v, 16))))
        pushParams(neonJSParams, 'Array', signableBytes);
        
		var sigs = cmdArgs[2].split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', sigs);
		//3 bigint[] xs
		var xs = cmdArgs[3].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', xs);
		//4 bigint[] ys
		var ys = cmdArgs[4].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', ys);
		var presHashMod = cmdArgs[8].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', presHashMod);

		await invokeOperation("savestate", neonJSParams, 500, 100)
var numSlices = 16
		//state pointmul sb
		//12 bigint[][] Qs_sb
		//13 bigint[] ss_sb
		//14 bigint[][] Ps_sb
		var pointMulData=fs.readFileSync('pointmulsteps', 'utf-8').split("||");


		//Second invoke, state pointmul ha
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

		await savePointMuls(Ps_ha, numSlices, "Ps_ha", "MULTI")
		await savePointMuls(ss_ha, numSlices, "ss_ha", "SIMPLE")
		await savePointMuls(Qs_ha, numSlices, "Qs_ha", "MULTI")

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

		await savePointMuls(Ps_sb, numSlices, "Ps_sb", "MULTI")
		await savePointMuls(ss_sb, numSlices, "ss_sb", "SIMPLE")
		await savePointMuls(Qs_sb, numSlices, "Qs_sb", "MULTI")
}

async function savePointMuls(arr, nchks, id, type) {
		var j = arr.length/nchks
		for (var i=0; i<nchks; i++) {
				neonJSParams = []
				pushParams(neonJSParams, 'Hex', portingContractID)
				pushParams(neonJSParams, 'String', type)
				pushParams(neonJSParams, 'String', id+String.fromCharCode(i))
				pushParams(neonJSParams, 'Array', arr.slice(i*j, (i+1)*j))

                await invokeOperation("savestate", neonJSParams, 50, 10)
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

function RegisterAsCollateral(){
    var neonJSParams = [];

    //0 byte[] collataddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] bncaddress
    var bncaddr = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
    pushParams(neonJSParams, 'Hex', bncaddr);
    pushParams(neonJSParams, 'Integer', 3000)
    pushParams(neonJSParams, 'Integer', 1)

    invokeOperation("registerAsCollateral", neonJSParams, 50, 10)
}

function NewPorting(){
    var neonJSParams = [];

    //0 byte[] collatid 
    pushParams(neonJSParams, 'Hex', collatid);
    //1 byte[] portingContractID
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    pushParams(neonJSParams, 'Integer', 40)
    pushParams(neonJSParams, 'String', "BNB")

    invokeOperation("newPorting", neonJSParams, 50, 10)
}

function ackUserDepositPorting(){
    var neonJSParams = [];

    //0 byte[] collatid 
    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID

    invokeOperation("ackDepositByUser", neonJSParams, 50, 10)
}

function executeChallenge(challengeNum){
    var neonJSParams = [];

    //0 byte[] collatid 
//    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID
  //  pushParams(neonJSParams, 'Address', ECO_WALLET._address);
	pushParams(neonJSParams, 'Hex', portingContractID)
    pushParams(neonJSParams, 'Integer', 6)
    pushParams(neonJSParams, 'Integer', 1)
    pushParams(neonJSParams, 'Integer', 0)
    pushParams(neonJSParams, 'String', "ha")

    invokeOperation("executeChallenge", neonJSParams, 155, 10)
}

collatid = "23ba2703c53263e8d6e522dc32203339dcd8eee9aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
portingContractID = "23ba2703c53263e8d6e522dc32203339dcd8eee9aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa23ba2703c53263e8d6e522dc32203339dcd8eee9bbbe885e"
scriptHash = "abd5a276856ba2f346a8326dc0dfe90777668cc1"

//RegisterAsCollateral()
//NewPorting()
//challengeDeposit()
//SaveState()
executeChallenge("1");//param commented in function
//pointmulchallenge num = 6
//initialchecks num =1


//ackUserDepositPorting()
//unlockCollateral()
//requestWithdraw()
//Challenge7()
//IsSaved()
//RemoveStorage()
//VerifyTxOutput()

//challengeWithdraw();
