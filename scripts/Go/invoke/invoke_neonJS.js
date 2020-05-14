const fs = require('fs');
const axios = require('axios');
const Neon = require("@cityofzion/neon-js");
var ECO_WALLET = new Neon.wallet.Account("KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr");
var scriptHash = "" //const
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

async function invokeOperation(operation, args, gas, fees, sh=scriptHash){

    let txr
    try {
        const response = await invoke(operation, args, gas, fees, sh)
        txr = (await getTxResult(response.txid, 2*60)).data.result//.executions[0]
        if (!txr) {
            console.log("Attemps exhausted")
            //retry
        }
        else{
            console.log(txr.executions[0])//return txr
            txr.executions[0].notifications.forEach(el => {
                console.log(el.state.value[0].value)
            });
        }
    } catch (error) {
        console.log(error.message)
    }
}

async function invoke(operation, args, gas, fees, sh){
    return Neon.default.doInvoke({
        api, // The API Provider that we rely on for balance and rpc information
        account: ECO_WALLET, // The sending Account
        gas, // Additional GAS for invocation.
        fees,
        script: Neon.default.create.script({ scriptHash:sh, operation, args })
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
		
        var pres = cmdArgs[6].split(" ").map(v => v.split(",").map(v=>Neon.sc.ContractParam.integer(maxBigInteger)))
        for (var j = 0; j< pres.length; j++) {
            for (var i = 0; i<32; i++) {
                pres[j] = []//.push(pres[j][0])
            }
        }
        console.log("leeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeen", pres[0].length)
        pushParams(neonJSParams, 'Array', pres);
        
		//7 ulong[][] preshash
        var presHash = cmdArgs[7].split(" ").map(v => v.split(",").map(v=>Neon.sc.ContractParam.integer(maxBigInteger)))
        
        console.log(presHash.length)
        console.log(presHash[0].length)
		pushParams(neonJSParams, 'Array', presHash);
        
        //9 byte[] txproof
        //console.log((cmdArgs[9]+"ff".repeat(500-196)).length)
		pushParams(neonJSParams, 'Hex', "aa")//cmdArgs[9])//+"ff".repeat(500-98));
		//10 byte[] blockHeader
        pushParams(neonJSParams, 'Hex', "aa")//cmdArgs[10])//+"ff".repeat(400- cmdArgs[10].length/2));

		//11 byte[] txbytes
        var txbytes = cmdArgs[11].split(",").map(v=>Neon.sc.ContractParam.integer(maxBigInteger))
        for (var i = txbytes.length; i<299; i++) {
            txbytes.push(txbytes[0])
        }
        console.log(txbytes.length)
		pushParams(neonJSParams, 'Array', txbytes);//[Neon.sc.ContractParam.integer(maxBigInteger)])

        await invokeOperation("savestate", neonJSParams, 50, 10)

	    neonJSParams = []	
		//1 byte[] porting
		pushParams(neonJSParams, 'Hex', portingContractID)
        pushParams(neonJSParams, 'String', "PM")
        
        var signableBytes = cmdArgs[5].split(",").map(v=>v.match(/.{1,2}/g).map(v=>Neon.sc.ContractParam.integer(parseInt(v, 16))))

        /*for (var j = 0; j< signableBytes.length; j++) {
            for (var i = signableBytes[j].length; i<200; i++) {
                signableBytes[j].push(signableBytes[j][0])
            }
        }*/
//        pushParams(neonJSParams, 'Array', signableBytes);

        
		var sigs = cmdArgs[2].split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', sigs);
		//3 bigint[] xs
		var xs = cmdArgs[3].split(",").map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
		pushParams(neonJSParams, 'Array', xs);
		//4 bigint[] ys
		var ys = cmdArgs[4].split(",").map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
		pushParams(neonJSParams, 'Array', ys);
		var presHashMod = cmdArgs[8].split(",").map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
		pushParams(neonJSParams, 'Array', presHashMod);

        await invokeOperation("savestate", neonJSParams, 500, 100)
console.log("signableBytes.length", signableBytes[0].length)

        for (var j = 0; j< signableBytes.length; j++) {
            neonJSParams = []
            pushParams(neonJSParams, 'Hex', portingContractID)
            pushParams(neonJSParams, 'String', "SIGNABLEBYTES")
            pushParams(neonJSParams, 'String', ""+String.fromCharCode(j))
            pushParams(neonJSParams, 'Array', signableBytes[j])
            await invokeOperation("savestate", neonJSParams, 500, 100)
        }
return
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
			ss_ha = ss_ha.concat(all.slice(0, 32).map(v =>Neon.sc.ContractParam.integer(maxBigInteger)))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
			Qs_ha = Qs_ha.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
			Ps_ha = Ps_ha.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

        /*await savePointMuls(Ps_ha, numSlices, "Ps_ha", "MULTI")
		await savePointMuls(ss_ha, numSlices, "ss_ha", "SIMPLE")
		await savePointMuls(Qs_ha, numSlices, "Qs_ha", "MULTI")*/

		var ss_sb = []
		var Ps_sb=[]
		var Qs_sb=[]
		var pointMuls_sb = pointMulData[0].split(" ")
		pointMuls_sb.map(v => {
			var all = v.split(",")
			ss_sb = ss_sb.concat(all.slice(0, 32).map(v => {return Neon.sc.ContractParam.integer(maxBigInteger)}))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
			Qs_sb = Qs_sb.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(v =>Neon.sc.ContractParam.integer(maxBigInteger))
			Ps_sb = Ps_sb.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

		/*await savePointMuls(Ps_sb, numSlices, "Ps_sb", "MULTI")
		await savePointMuls(ss_sb, numSlices, "ss_sb", "SIMPLE")
		await savePointMuls(Qs_sb, numSlices, "Qs_sb", "MULTI")*/
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
    pushParams(neonJSParams, 'Integer', 7)
    pushParams(neonJSParams, 'Integer', 1)
    //pushParams(neonJSParams, 'String', ""+String.fromCharCode(0))
    pushParams(neonJSParams, 'Integer', 0)
    pushParams(neonJSParams, 'String', "sb")

    invokeOperation("executeChallenge", neonJSParams, 200, 10)
}

collatid = "23ba2703c53263e8d6e522dc32203339dcd8eee9aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
portingContractID = "23ba2703c53263e8d6e522dc32203339dcd8eee9aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa23ba2703c53263e8d6e522dc32203339dcd8eee9bbbe885e"
scriptHash = "c1384e12e410cc6a58677324b5ac14ad255684e4"

var neonJSParams = [];
var s = "2fffffffffffffffff"//.repeat(7)
var maxBigInteger = parseInt(s, 16)
//RegisterAsCollateral()
//NewPorting()
//challengeDeposit()
//SaveState()
executeChallenge("1");//param commented in function
//pointmulchallenge num = 6
//initialchecks num =1
//pushParams(neonJSParams, 'Integer', parseInt(s, 16))
//invokeOperation("hi", neonJSParams, 50, 10, "367514d58a867ceabd4c4522a661d999af72e8b3")

//ackUserDepositPorting()
//unlockCollateral()
//requestWithdraw()
//Challenge7()
//IsSaved()
//RemoveStorage()
//VerifyTxOutput()
//MAX BYTE ARRAY = "FF"*65507
//challengeWithdraw();