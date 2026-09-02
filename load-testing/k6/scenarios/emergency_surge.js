import http from 'k6/http';
import { check, sleep } from 'k6';

// 500 Virtual Users Simulation (Ramp-up, Surge, Cooldown)
export const options = {
  stages: [
    { duration: '20s', target: 50 },
    { duration: '30s', target: 500 },
    { duration: '1m', target: 500 },
    { duration: '20s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<300', 'p(99)<800'],
    http_req_failed: ['rate<0.001'],
  },
};

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5000';
const TOKEN = __ENV.AUTH_TOKEN || '';

export default function () {
  const params = {
    headers: {
      'Content-Type': 'application/json',
      ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}),
    },
    responseCallback: http.expectedStatuses(200, 201, 400, 401),
  };

  // 1. Paginated incident retrieval (SLA: p95 < 300ms)
  const getRes = http.get(`${BASE_URL}/api/v1/incidents?pageNumber=1&pageSize=20`, params);
  check(getRes, {
    'GET /incidents status 200 or 401': (r) => r.status === 200 || r.status === 401,
    'GET latency under 300ms': (r) => r.timings.duration < 300,
  });

  // 2. Teams status check
  const teamsRes = http.get(`${BASE_URL}/api/v1/teams`, params);
  check(teamsRes, {
    'GET /teams responded': (r) => r.status === 200 || r.status === 401,
  });

  // 3. 30% probability of reporting a new emergency incident
  if (Math.random() < 0.3) {
    const payload = JSON.stringify({
      category: 'Fire',
      emergencyCode: 'RED_ALERT',
      description: 'Simulated high-load emergency surge incident',
      latitude: 38.7900 + (Math.random() * 0.01 - 0.005),
      longitude: 26.9200 + (Math.random() * 0.01 - 0.005),
      mediaAttachments: []
    });

    const postRes = http.post(`${BASE_URL}/api/v1/incidents`, payload, params);
    check(postRes, {
      'POST /incidents handled': (r) => r.status === 200 || r.status === 201 || r.status === 400 || r.status === 401,
    });
  }

  sleep(1);
}
