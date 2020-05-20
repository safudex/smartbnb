import { u } from '@cityofzion/neon-js';
import { promisify } from "util";
import redis from 'redis';
import NeoApi, { NotificationMessage } from './neolib';
import BncApi from './bnclib';

function neoEventHandler(neo: NeoApi, redisUrl:string, collatId: string, bnc:BncApi) {
  const redisClient = redis.createClient(redisUrl);
  const get:(key:string)=>Promise<string> = promisify(redisClient.get).bind(redisClient); // get("key")
  const set:(key:string, value:string)=>Promise<"OK"> = promisify(redisClient.set).bind(redisClient); // set("key", "value")
  return async (event: NotificationMessage) => {
    const operation = u.hexstring2str(event.event[0].value);
    if(operation === 'priceupdated'){
        const newPrice:number = hex2int(event.event[1].value)
        const collateral = str2int(await get("collateral"));
        const collateralizedToken = str2int(await get("collateralizedToken"));
        // Check
        return;
    }
    const rellevantCollatId = u.hexstring2str(event.event[1].value);
    if(rellevantCollatId !== collatId){
        return; // ignore event
    }
    switch (operation) {
      case 'portrequestcreated':
        //
        break;
      case 'withdrawrequestcreated':
        bnc.send()
        //
        break;
      case 'challengewithdrawcreated':
        //
        break;
      case 'challengedepositcreated':
        //
        break;
      case 'priceupdated':
        // Check that we have enough collateral
        break;
    }
  };
}

function hex2int(hex:string):number{
    const reverseHex = (hex:string) => hex.match(/.{2}/g).reverse().join('');
    return parseInt(reverseHex(hex), 16);
}

function str2int(str:string):number{
    return parseInt(str);
}

function int2str(int:number):string{
    return JSON.stringify(int);
}

export default neoEventHandler;
