import ws from 'k6/ws';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '15s', target: 50 },
    { duration: '30s', target: 200 },
    { duration: '1m', target: 200 },
    { duration: '15s', target: 0 },
  ],
  thresholds: {
    checks: ['rate>0.95'],
  },
};

const WS_BASE_URL = __ENV.SIGNALR_WS_URL || 'ws://localhost:5000';
const TOKEN = __ENV.AUTH_TOKEN || '';

export default function () {
  const url = `${WS_BASE_URL}/hubs/location${TOKEN ? `?access_token=${TOKEN}` : ''}`;
  const RECORD_SEPARATOR = String.fromCharCode(0x1e);

  const res = ws.connect(url, {}, function (socket) {
    socket.on('open', function () {
      // Handshake with SignalR Hub Protocol
      socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + RECORD_SEPARATOR);

      // Periodic GPS location stream every 1 second
      socket.setInterval(function () {
        const teamId = '11111111-1111-1111-1111-111111111111';
        const lat = 38.7915 + (Math.random() * 0.002 - 0.001);
        const lng = 26.9212 + (Math.random() * 0.002 - 0.001);

        const invocation = {
          type: 1,
          target: 'StreamTeamLocation',
          arguments: [teamId, lat, lng]
        };

        socket.send(JSON.stringify(invocation) + RECORD_SEPARATOR);
      }, 1000);
    });

    socket.on('message', function () {
      // Inbound telemetry broadcast received
    });

    socket.setTimeout(function () {
      socket.close();
    }, 30000);
  });

  check(res, {
    'WebSocket Connected Successfully': (r) => r && r.status === 101,
  });

  sleep(1);
}
