import BnbApiClient from '@binance-chain/javascript-sdk';
import WebSocket from 'ws';
import axios from 'axios';

class BncApi {
  private bnbClient: BnbApiClient;

  private apiUrl: string;

  constructor(privateKey: string, network: 'MainNet' | 'TestNet', public readonly asset: string) {
    this.apiUrl = {
      MainNet: 'dex.binance.org/',
      TestNet: 'testnet-dex.binance.org/',
    }[network];
    this.bnbClient = new BnbApiClient(`https://${this.apiUrl}`);
    // @ts-ignore Typescript doens't understand that lowercase of 'MainNet' | 'TestNet' becomes "testnet"|"mainnet"
    this.bnbClient.chooseNetwork(network.toLowerCase());
    this.bnbClient.setPrivateKey(privateKey);
    this.bnbClient.initChain();
  }

  public async send(addressTo: string, amount: number):Promise<string> {
    const addressFrom = this.bnbClient.getClientKeyAddress(); // sender address string (e.g. bnb1...)
    const sequenceURL = `https://${this.apiUrl}api/v1/account/${addressFrom}/sequence`;
    const httpClient = axios.create({ baseURL: `https://${this.apiUrl}` });
    return httpClient
      .get(sequenceURL)
      .then((res) => {
        const sequence = res.data.sequence || 0;
        return this.bnbClient.transfer(addressFrom, addressTo, amount, this.asset, '', sequence);
      })
      .then((result) => {
        if (result.status === 200) {
          // See https://github.com/binance-chain/javascript-sdk/blob/9e72debcbe77e1f8807671de5af9070079b8d13b/src/utils/request.js#L19
          // and https://docs.binance.org/api-reference/dex-api/paths.html#transaction
          return result.result.hash;
        }
        throw Error(`Transaction failed: ${JSON.stringify(result)}`);
      });
  }

  public subscribe(callback: (msg: WebSocket.Data) => void) {
    const subsURL = `wss://${this.apiUrl}api/ws/${this.address}`;
    const ws = new WebSocket(subsURL);
    ws.on('message', callback);
  }

  get address():string {
    return this.bnbClient.getClientKeyAddress();
  }
}

export default BncApi;
