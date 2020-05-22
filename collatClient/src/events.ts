import { u } from '@cityofzion/neon-js';
import { promisify } from 'util';
import redis from 'redis';
import NeoApi, { NotificationMessage } from './neolib';
import BncApi from './bnclib';
import Emailer from './emailer';
import hex2int from './neoutils';

function neoEventHandler(neo: NeoApi, redisUrl: string, collatId: string, bnc: BncApi, emailAddress: string) {
  const emailer = new Emailer(emailAddress, '"Collateral provider" <collateral@smartbnb.net>', 'Updates on your collateral provider');
  const redisClient = redis.createClient(redisUrl);
  const get: (key: string) => Promise<string> = promisify(redisClient.get).bind(redisClient); // get("key")
  const set: (key: string, value: string) => Promise<unknown> = promisify(redisClient.set).bind(redisClient); // set("key", "value")

  return async (event: NotificationMessage) => {
    const operation = u.hexstring2str(event.event[0].value);
    const eventCollatId = u.hexstring2str(event.event[1].value);
    if (eventCollatId !== collatId) {
      return; // ignore event
    }
    const portingContractId = u.hexstring2str(event.event[2].value);
    switch (operation) {
      case 'portrequestcreated': {
        const userAddr = u.hexstring2str(event.event[3].value);
        const amountRequested = hex2int(event.event[4]);
        bnc.subscribe((msg) => {
          const bncEvent = JSON.parse(msg.toString());
          if (bncEvent.stream !== 'transfers') {
            return;
          }
          const assetTransferred = bncEvent.data.t[0].c[0].a;
          const amountTransferred = parseFloat(bncEvent.data.t[0].c[0].A);
          const from = bncEvent.data.f;
          const to = bncEvent.data.t[0].o;
          if (
            userAddr === from
                        && bnc.address === to
                        && bnc.asset === assetTransferred
                        && amountRequested === ((amountTransferred * 1e8) % 1) // There shoudn't be any problem caused by float imprecision because both numbers are ints which are way below the SAFE_MAX_INT defined by the js standard
          ) {
            // We could kill the subscription here but we don't need to because it is not a problem due to the fact that a deposit cannot be re-acked
            neo.invoke('ackDepositByUser', [portingContractId]);
          }
        });
        break;
      }
      case 'withdrawrequestcreated': {
        const userBCNAddr = u.hexstring2str(event.event[3].value);
        const withdrawAmountRequested = hex2int(event.event[4]); // Again, no float imprecision because of the reasons stated in the comment above
        const txHash = await bnc.send(userBCNAddr, withdrawAmountRequested);
        await set(portingContractId, txHash); // Store txhash
        break;
      }
      case 'challengewithdrawcreated': {
        const sentTxHash = await get(portingContractId);
        await neo.sendProof(sentTxHash, portingContractId);
        break;
      }
      case 'challengedepositcreated': {
        // Send email alerting user
        emailer.send(`A deposit challenge has been raised against your collateral provider, you should monitor the smart contract and wait for the challenging user to submit a proof before 24 hours, afterwards, you will need to verify the steps and submit a counter-proof. The counter-proof must be submitted before 48 hours have passed since the receiving of this email, but not before 24 hours of it. Failure to provide the counter-proof will result in the collateral of your collateral provider getting slashed, as well as a security deposit.
                Extra information:
                Porting contract: ${portingContractId}
                CollatId: ${collatId}`);
        break;
      }
    }
  };
}

export default neoEventHandler;
