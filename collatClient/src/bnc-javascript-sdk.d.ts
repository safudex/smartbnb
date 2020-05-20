declare module '@binance-chain/javascript-sdk' {
    export default class BnbApiClient {
      constructor(url:string);

      chooseNetwork(network: 'testnet'|'mainnet'):void;

      setPrivateKey(privateKey:string):void;

      initChain():void;

      getClientKeyAddress():string;

      transfer(addressFrom:string, addressTo:string, amount:number, asset:string, message:string, sequence:number):Promise<any>;
    }
}
