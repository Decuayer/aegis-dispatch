# Changelog

All notable changes to the **AegisDispatch** platform will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned / In Progress (Sprints 1 – 4 Backlog)
* **SDDC-68 & SDDC-74:** Relational database schema and S3 storage handlers for user operational feedbacks and multi-media attachments.
* **SDDC-69 & SDDC-70:** Generic CQRS server-side pagination engine (`PagedResult<T>`) and composite B-Tree indexing on `Incidents`, `Teams`, and `Users`.
* **SDDC-71 & SDDC-72:** Server-side filtering and ID lookup endpoints for incidents, response teams, and personnel directories.
* **SDDC-73 & SDDC-82:** Team discovery portal, responder self-join/leave workflows, and vacant leadership succession protocols.
* **SDDC-77 & SDDC-86:** Entity ID badges (`#INC-XXXX`, `#TEAM-XX`) across Web Leaflet markers and Flutter mobile cards.
* **SDDC-78 & SDDC-86:** Facility boundary geofence lock on Leaflet and Flutter map viewports (industrial facility perimeter coordinates).
* **SDDC-79:** First-launch KVKK privacy consent and system location permission onboarding gate for mobile users.
* **SDDC-80:** Single-button rapid emergency incident reporting (Fire, Gas Leak, Medical Emergency, SOS) with background GPS capture.
* **SDDC-83:** 1-Click operational dispatch action bar for response units (`En Route`, `On Scene`).
* **SDDC-84:** Multi-photo and multi-video capture and thumbnail generation pipeline on mobile.

---

## [1.0.0] - 2026-08-20

### Added
* **Backend Architecture (.NET 8 Core):**
  * Clean Architecture layers (`Domain`, `Application`, `Infrastructure`, `API`).
  * CQRS pattern implementation powered by MediatR and FluentValidation pipeline behaviors.
  * Relational entity modeling for `Users`, `Teams`, `TeamMembers`, `Incidents`, and `Assignments`.
* **Real-Time Telemetry & Communication:**
  * SignalR `IncidentHub` (`/hubs/incidents`) for instant incident broadcast and status transition notifications.
  * SignalR `LocationHub` (`/hubs/location`) for sub-second GPS coordinate stream transmission.
* **Geospatial & Persistence Layer:**
  * PostgreSQL 16 database with PostGIS geospatial extension enabled.
  * Spatial data indexing and geospatial query foundation (`ST_DWithin`, `ST_Distance`).
  * Redis distributed caching integration for transient responder telemetry.
  * S3-compatible MinIO object storage bucket automation for incident media files.
* **Security & Access Control:**
  * Role-Based Access Control (RBAC) supporting `Employee`, `Team`, and `Operator` authorization policies.
  * Secure JWT authentication scheme with refresh token generation.
* **Client Applications:**
  * **Flutter Mobile App:** Cross-platform mobile client for field employees and response teams, featuring background GPS telemetry and incident submission.
  * **Blazor Web GIS Console:** Dispatch operator web portal integrating Leaflet interactive map, real-time incident queue, and fleet assignment panel.
* **DevOps & Infrastructure:**
  * Docker Compose setup for localized PostgreSQL/PostGIS, Redis, and MinIO instances.
  * Automated database initialization scripts (`docker/init-db`).
