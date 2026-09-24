# AegisDispatch Performance & Load Testing Suite

This directory contains the **k6** performance and stress simulation suite for **AegisDispatch**, validating system resilience under plant-wide emergencies according to NFR-01 and NFR-02 requirements.

## Scenarios

1. **`emergency_surge.js`:** Simulates facility-wide emergency scenarios with 500+ concurrent frontline employees reporting incidents while dispatch operators query paginated triage lists.
   - **Target SLA:** $p_{95} < 300\text{ ms}$, $p_{99} < 800\text{ ms}$, Error rate $< 0.1\%$.
2. **`websocket_location_stream.js`:** 200 field response teams concurrently streaming live GPS coordinates over SignalR `/hubs/location` every second.

## Prerequisites

- [k6 CLI](https://k6.io/docs/get-started/installation/):
  ```bash
  # macOS
  brew install k6
  ```

## Execution

```bash
# Execute entire load testing suite
./load-testing/k6/run-load-tests.sh
```
