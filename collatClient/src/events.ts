import { u } from '@cityofzion/neon-js';
import redis from 'redis';
import NeoApi, { NotificationMessage } from './neolib';
import BncApi from './bnclib';

function neoEventHandler(neo: NeoApi, redisUrl:string, collatId: string, bnc:BncApi) {
  const redisClient = redis.createClient(redisUrl);
  return (event: NotificationMessage) => {
    const operation = u.hexstring2str(event.event[0].value);

    switch (operation) {
      case 'portrequestcreated':
        //
        break;
      case 'withdrawrequestcreated':
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

export default neoEventHandler;
