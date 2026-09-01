// wwwroot/js/leafletMap.js
window.leafletMap = (function () {
    let map = null;
    let incidentClusterGroup = null;      // Layer for active incidents
    let resolvedClusterGroup = null;      // Layer for resolved/canceled incidents
    let teamLayerGroup = null;
    let incidentMarkers = {}; // { [incidentId]: marker }
    let teamMarkers = {};     // { [teamId]: marker }

    // Picker Mini-Map instances
    let pickerMap = null;
    let pickerMarker = null;
    let _pickerDotNetRef = null;
    let pickerTileLayer = null;

    // SOCAR Aliaga / STAR Refinery facility bounding box
    const SOCAR_FACILITY_BOUNDS = [
        [38.7650, 26.8900], // South-West (Lat, Lng)
        [38.8350, 26.9750]  // North-East (Lat, Lng)
    ];
    const DEFAULT_LOCKED_MIN_ZOOM = 13;
    const DEFAULT_UNLOCKED_MIN_ZOOM = 0;

    // Get color according to EmergencyCode or Status
    function getIncidentColor(emergencyCode, status) {
        if (status === 'Resolved') return '#22c55e'; // Green
        if (status === 'Canceled') return '#6b7280'; // Gray
        const code = (emergencyCode || '').toLowerCase();
        if (code.includes('kirmizi') || code.includes('red') || code.includes('1'))
            return '#ef4444';
        if (code.includes('turuncu') || code.includes('orange') || code.includes('2'))
            return '#f97316';
        if (code.includes('sari') || code.includes('yellow') || code.includes('3'))
            return '#f59e0b';
        return '#3b82f6';
    }

    // Custom SVG-based incident marker icon
    function createIncidentIcon(emergencyCode, status) {
        const color = getIncidentColor(emergencyCode, status);
        const isAssigned = (status === 'Assigned');
        const isResolved = (status === 'Resolved');
        const isCanceled = (status === 'Canceled');

        let iconSymbol = '!';
        if (isResolved) iconSymbol = '✓';
        else if (isCanceled) iconSymbol = '✕';
        else if (isAssigned) iconSymbol = '⚡';

        // Assigned dispatch badge indicator
        const assignedBadge = isAssigned ? `
          <circle cx="25" cy="7" r="6" fill="#3b82f6" stroke="white" stroke-width="1.5"/>
          <text x="25" y="10" text-anchor="middle" font-size="8" fill="white" font-weight="bold" font-family="Inter,sans-serif">A</text>
        ` : '';

        const strokeProps = isAssigned ? 'stroke="#60a5fa" stroke-width="2.5" stroke-dasharray="3,2"' : 'stroke="white" stroke-width="2"';

        const svg = `
          <svg xmlns="http://www.w3.org/2000/svg" width="34" height="42" viewBox="0 0 34 42">
            <path d="M16 2C7.16 2 0 9.16 0 18c0 10 16 24 16 24S32 28 32 18C32 9.16 24.84 2 16 2z"
                  fill="${color}" ${strokeProps}/>
            <text x="16" y="23" text-anchor="middle" font-size="14" fill="white" font-weight="bold"
                  font-family="Inter,sans-serif">${iconSymbol}</text>
            ${assignedBadge}
          </svg>`;

        return L.divIcon({
            html: svg,
            className: isAssigned ? 'lf-marker-assigned' : '',
            iconSize: [34, 42],
            iconAnchor: [16, 42],
            popupAnchor: [0, -42]
        });
    }

    // Color according to team status
    function getTeamColor(status) {
        switch (status) {
            case 'Idle':      return '#22c55e';
            case 'Forwarded': return '#3b82f6';
            case 'OnScene':   return '#f59e0b';
            case 'Busy':      return '#ef4444';
            default:          return '#6b7280';
        }
    }

    // SVG-based custom team marker icon
    function createTeamIcon(status) {
        const color = getTeamColor(status);
        const svg = `
          <svg xmlns="http://www.w3.org/2000/svg" width="34" height="34" viewBox="0 0 34 34">
            <circle cx="17" cy="17" r="15" fill="${color}" stroke="white" stroke-width="2"/>
            <text x="17" y="22" text-anchor="middle" font-size="14" fill="white" font-weight="bold"
                  font-family="Inter,sans-serif">T</text>
          </svg>`;
        return L.divIcon({
            html: svg,
            className: '',
            iconSize: [34, 34],
            iconAnchor: [17, 17],
            popupAnchor: [0, -17]
        });
    }

    let mainTileLayer = null;

    // Initialize the map
    function initMap(containerId, lat, lng, zoom, tileProvider) {
        if (map) {
            map.remove();
            map = null;
            mainTileLayer = null;
        }

        map = L.map(containerId, {
            center: [lat, lng],
            zoom: zoom || 14,
            zoomControl: true,
            attributionControl: true
        });

        // Dynamic tile layer provider
        const tileUrl = getTileUrl(tileProvider);
        mainTileLayer = L.tileLayer(tileUrl, {
            attribution: '© OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(map);

        // Active incidents cluster group
        incidentClusterGroup = L.markerClusterGroup({
            maxClusterRadius: 60,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false
        });
        map.addLayer(incidentClusterGroup);

        // Resolved incidents cluster group (hidden by default)
        resolvedClusterGroup = L.markerClusterGroup({
            maxClusterRadius: 60,
            spiderfyOnMaxZoom: true,
            showCoverageOnHover: false
        });

        // Normal layer group — for teams
        teamLayerGroup = L.layerGroup().addTo(map);
    }

    function updateMainTileLayer(tileProvider) {
        if (!map || !mainTileLayer) return;
        map.removeLayer(mainTileLayer);
        mainTileLayer = L.tileLayer(getTileUrl(tileProvider), { maxZoom: 19 }).addTo(map);
        mainTileLayer.bringToBack();
    }

    // Copy text to clipboard with fallback and visual feedback
    function copyToClipboard(text, element) {
        if (!text) return;
        const cleanText = text.trim();
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(cleanText)
                .then(() => showCopyFeedback(element))
                .catch(() => fallbackCopy(cleanText, element));
        } else {
            fallbackCopy(cleanText, element);
        }
    }

    function fallbackCopy(text, element) {
        const tempInput = document.createElement('input');
        tempInput.value = text;
        document.body.appendChild(tempInput);
        tempInput.select();
        document.execCommand('copy');
        document.body.removeChild(tempInput);
        showCopyFeedback(element);
    }

    function showCopyFeedback(el) {
        if (!el) return;
        const originalHtml = el.innerHTML;
        el.classList.add('copied');
        el.innerHTML = `✓ Copied!`;
        setTimeout(() => {
            el.classList.remove('copied');
            el.innerHTML = originalHtml;
        }, 1500);
    }

    // Helper: Incident Popup HTML Template Generator
    function createIncidentPopupHtml(data) {
        const { id, category, emergencyCode, reporterFullName, status, createdAt, assignedTeamName } = data;
        const formattedDate = new Date(createdAt).toLocaleString('tr-TR');
        const color = getIncidentColor(emergencyCode, status);
        const shortId = id ? id.substring(0, 8).toUpperCase() : 'UNKNOWN';

        const teamInfo = assignedTeamName
            ? `<div class="lf-popup-row"><span class="lf-label">Assigned Team:</span> <span class="lf-value">${assignedTeamName}</span></div>`
            : `<div class="lf-popup-row lf-unassigned">No team assigned yet</div>`;

        return `
          <div class="lf-popup">
            <div class="lf-popup-header" style="border-color: ${color}">
              <span class="lf-emergency-badge" style="background:${color}">${emergencyCode}</span>
              <strong>${category}</strong>
            </div>
            <div class="lf-popup-body">
              <div class="lf-popup-id-row">
                <span class="badge-entity-id" 
                      title="Full ID: ${id} (Click to copy)" 
                      onclick="window.leafletMap.copyToClipboard('${id}', this)">
                  ID: #${shortId} <span class="copy-icon">📋</span>
                </span>
              </div>
              <div class="lf-popup-row"><span class="lf-label">Reporter:</span> <span class="lf-value">${reporterFullName}</span></div>
              <div class="lf-popup-row"><span class="lf-label">Status:</span> <span class="lf-value lf-status-${status.toLowerCase()}">${status}</span></div>
              <div class="lf-popup-row"><span class="lf-label">Time:</span> <span class="lf-value">${formattedDate}</span></div>
              ${teamInfo}
            </div>
            <div class="lf-popup-actions">
              <button class="lf-btn-primary" onclick="window.leafletMap.onIncidentDetailClick('${id}')">View Details</button>
              <button class="lf-btn-ghost" onclick="window.leafletMap.onAssignTeamClick('${id}')">Assign Team</button>
            </div>
          </div>
        `;
    }


    // Add or update incident marker
    function addIncidentMarker(incident) {
        if (!map) return;
        const { id, lat, lng, category, emergencyCode, status } = incident;

        const isResolved = status === 'Resolved' || status === 'Canceled';
        const targetGroup = isResolved ? resolvedClusterGroup : incidentClusterGroup;

        // If marker already exists, clean up from groups and update
        if (incidentMarkers[id]) {
            const existingMarker = incidentMarkers[id];
            if (incidentClusterGroup && incidentClusterGroup.hasLayer(existingMarker)) {
                incidentClusterGroup.removeLayer(existingMarker);
            }
            if (resolvedClusterGroup && resolvedClusterGroup.hasLayer(existingMarker)) {
                resolvedClusterGroup.removeLayer(existingMarker);
            }

            existingMarker.setLatLng([lat, lng]);
            existingMarker.setIcon(createIncidentIcon(emergencyCode, status));
            existingMarker._incidentData = { ...existingMarker._incidentData, ...incident };
            existingMarker.setPopupContent(createIncidentPopupHtml(existingMarker._incidentData));
            
            if (targetGroup) targetGroup.addLayer(existingMarker);
            return;
        }

        const marker = L.marker([lat, lng], {
            icon: createIncidentIcon(emergencyCode, status),
            title: `${category} — ${emergencyCode}`
        });

        marker._incidentData = { ...incident };

        marker.bindPopup(createIncidentPopupHtml(incident), { 
            maxWidth: 280, 
            className: 'lf-custom-popup' 
        });

        if (targetGroup) targetGroup.addLayer(marker);
        incidentMarkers[id] = marker;
    }

    // Helper: Team Popup HTML Template Generator
    function createTeamPopupHtml(team) {
        const { id, teamName, status, updatedAt } = team;
        const shortId = id ? id.substring(0, 8).toUpperCase() : 'UNKNOWN';
        const formattedDate = new Date(updatedAt).toLocaleTimeString('tr-TR');
        const color = getTeamColor(status);

        return `
          <div class="lf-popup">
            <div class="lf-popup-header" style="border-color: ${color}">
              <span class="lf-status-badge lf-status-${status.toLowerCase()}">${status}</span>
              <strong>${teamName}</strong>
            </div>
            <div class="lf-popup-body">
              <div class="lf-popup-id-row">
                <span class="badge-entity-id" 
                      title="Full ID: ${id} (Click to copy)" 
                      onclick="window.leafletMap.copyToClipboard('${id}', this)">
                  ID: #${shortId} <span class="copy-icon">📋</span>
                </span>
              </div>
              <div class="lf-popup-row"><span class="lf-label">Status:</span> <span class="lf-value lf-status-${status.toLowerCase()}">${status}</span></div>
              <div class="lf-popup-row"><span class="lf-label">Updated:</span> <span class="lf-value">${formattedDate}</span></div>
            </div>
          </div>
        `;
    }


    // Add / update team marker
    function addTeamMarker(team) {
        if (!map) return;
        const { id, teamName, status, lat, lng, updatedAt } = team;

        if (!lat || !lng) return;

        const shortTeamId = id ? id.substring(0, 8).toUpperCase() : 'UNKNOWN';

        if (teamMarkers[id]) {
            teamMarkers[id].setLatLng([lat, lng]);
            teamMarkers[id].setIcon(createTeamIcon(status));
            teamMarkers[id]._teamData = { ...teamMarkers[id]._teamData, ...team };
            teamMarkers[id].setTooltipContent(`
                <div class="lf-tooltip">
                  <strong>${teamName}</strong>
                  <span class="badge-entity-id" style="width:fit-content; margin: 2px 0;">Team ID: #${shortTeamId}</span>
                  <span class="lf-status-badge lf-status-${status.toLowerCase()}">${status}</span>
                  <small>Updated: ${new Date(updatedAt).toLocaleTimeString('tr-TR')}</small>
                </div>
            `);
            teamMarkers[id].setPopupContent(createTeamPopupHtml(teamMarkers[id]._teamData));
            return;
        }

        const marker = L.marker([lat, lng], {
            icon: createTeamIcon(status),
            title: teamName
        });

        marker._teamData = { ...team };

        marker.bindTooltip(`
            <div class="lf-tooltip">
              <strong>${teamName}</strong>
              <span class="badge-entity-id" style="width:fit-content; margin: 2px 0;">Team ID: #${shortTeamId}</span>
              <span class="lf-status-badge lf-status-${status.toLowerCase()}">${status}</span>
              <small>Updated: ${new Date(updatedAt).toLocaleTimeString('tr-TR')}</small>
            </div>
        `, { permanent: false, direction: 'top', className: 'lf-custom-tooltip' });

        marker.bindPopup(createTeamPopupHtml(team), {
            maxWidth: 260,
            className: 'lf-custom-popup'
        });

        teamLayerGroup.addLayer(marker);
        teamMarkers[id] = marker;
    }


    function animateTeamMarker(teamId, targetLat, targetLng) {
        const marker = teamMarkers[teamId];
        if (!marker) return;

        if (marker._animFrameId) {
            cancelAnimationFrame(marker._animFrameId);
        }

        const start = marker.getLatLng();
        const duration = 1000;
        const startTime = performance.now();

        function animate(currentTime) {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeOut = 1 - Math.pow(1 - progress, 3);
            const currentLat = start.lat + (targetLat - start.lat) * easeOut;
            const currentLng = start.lng + (targetLng - start.lng) * easeOut;

            marker.setLatLng([currentLat, currentLng]);

            if (progress < 1) {
                marker._animFrameId = requestAnimationFrame(animate);
            } else {
                marker._animFrameId = null;
            }
        }

        marker._animFrameId = requestAnimationFrame(animate);
    }

    // Remove marker
    function removeIncidentMarker(incidentId) {
        if (incidentMarkers[incidentId]) {
            const m = incidentMarkers[incidentId];
            if (incidentClusterGroup && incidentClusterGroup.hasLayer(m)) incidentClusterGroup.removeLayer(m);
            if (resolvedClusterGroup && resolvedClusterGroup.hasLayer(m)) resolvedClusterGroup.removeLayer(m);
            delete incidentMarkers[incidentId];
        }
    }

    function removeTeamMarker(teamId) {
        if (teamMarkers[teamId]) {
            teamLayerGroup.removeLayer(teamMarkers[teamId]);
            delete teamMarkers[teamId];
        }
    }

    function updateIncidentStatus(incidentId, newStatus) {
        const marker = incidentMarkers[incidentId];
        if (!marker) return;

        if (marker._incidentData) {
            marker._incidentData.status = newStatus;
            addIncidentMarker(marker._incidentData); // Re-adds to appropriate active/resolved group
        }
    }

    function updateTeamStatus(teamId, newStatus) {
        const marker = teamMarkers[teamId];
        if (!marker) return;

        marker.setIcon(createTeamIcon(newStatus));

        if (marker._teamData) {
            marker._teamData.status = newStatus;
            marker._teamData.updatedAt = new Date().toISOString();
            const shortTeamId = teamId ? teamId.substring(0, 8).toUpperCase() : 'UNKNOWN';

            marker.setTooltipContent(`
                <div class="lf-tooltip">
                  <strong>${marker._teamData.teamName}</strong>
                  <span class="badge-entity-id" style="width:fit-content; margin: 2px 0;">Team ID: #${shortTeamId}</span>
                  <span class="lf-status-badge lf-status-${newStatus.toLowerCase()}">${newStatus}</span>
                  <small>Updated: ${new Date().toLocaleTimeString('tr-TR')}</small>
                </div>
            `);
            marker.setPopupContent(createTeamPopupHtml(marker._teamData));
        }
    }


    function updateIncidentAssignment(incidentId, teamId, teamName) {
        const marker = incidentMarkers[incidentId];
        if (marker && marker._incidentData) {
            marker._incidentData.assignedTeamName = teamName;
            marker._incidentData.status = 'Assigned';
            marker.setPopupContent(createIncidentPopupHtml(marker._incidentData));
        }

        if (teamId) {
            updateTeamStatus(teamId, 'Forwarded');
        }
    }

    function panToIncident(incidentId) {
        if (incidentMarkers[incidentId]) {
            const marker = incidentMarkers[incidentId];
            if (resolvedClusterGroup && resolvedClusterGroup.hasLayer(marker) && !map.hasLayer(resolvedClusterGroup)) {
                map.addLayer(resolvedClusterGroup);
            }
            if (incidentClusterGroup && incidentClusterGroup.hasLayer(marker) && !map.hasLayer(incidentClusterGroup)) {
                map.addLayer(incidentClusterGroup);
            }
            const latlng = marker.getLatLng();
            map.flyTo(latlng, 16, { animate: true, duration: 1.2 });
            marker.openPopup();
        }
    }

    function panToLocation(lat, lng, zoom) {
        if (map) map.flyTo([lat, lng], zoom || 15, { animate: true });
    }

    function panToTeam(teamId) {
        if (teamMarkers[teamId]) {
            const latlng = teamMarkers[teamId].getLatLng();
            map.flyTo(latlng, 16, { animate: true, duration: 1.5 });
            teamMarkers[teamId].openTooltip();
        }
    }

    function focusMarkerById(entityType, entityId, zoomLevel) {
        if (!map || !entityId) return false;
        const zoom = zoomLevel || 17;
        const type = (entityType || '').toLowerCase();

        let targetMarker = null;

        if (type === 'incident') {
            targetMarker = incidentMarkers[entityId];
            if (targetMarker) {
                if (resolvedClusterGroup && resolvedClusterGroup.hasLayer(targetMarker) && !map.hasLayer(resolvedClusterGroup)) {
                    map.addLayer(resolvedClusterGroup);
                }
                if (incidentClusterGroup && incidentClusterGroup.hasLayer(targetMarker) && !map.hasLayer(incidentClusterGroup)) {
                    map.addLayer(incidentClusterGroup);
                }

                // If clustered, uncluster and reveal popup
                const cluster = incidentClusterGroup && incidentClusterGroup.hasLayer(targetMarker) ? incidentClusterGroup : resolvedClusterGroup;
                if (cluster && typeof cluster.zoomToShowLayer === 'function') {
                    cluster.zoomToShowLayer(targetMarker, () => {
                        targetMarker.openPopup();
                        applyMarkerPulse(targetMarker);
                    });
                    return true;
                }
            }
        } else if (type === 'team') {
            targetMarker = teamMarkers[entityId];
            if (targetMarker && teamLayerGroup && !map.hasLayer(teamLayerGroup)) {
                map.addLayer(teamLayerGroup);
            }
        }

        if (targetMarker) {
            const latlng = targetMarker.getLatLng();
            map.flyTo(latlng, zoom, { animate: true, duration: 1.2 });

            if (targetMarker.openPopup) {
                targetMarker.openPopup();
            } else if (targetMarker.openTooltip) {
                targetMarker.openTooltip();
            }

            applyMarkerPulse(targetMarker);
            return true;
        }

        return false;
    }

    function applyMarkerPulse(marker) {
        if (!marker || !marker._icon) return;
        const iconEl = marker._icon;
        iconEl.classList.remove('lf-marker-focus-pulse');
        void iconEl.offsetWidth; // Trigger reflow for animation restart
        iconEl.classList.add('lf-marker-focus-pulse');
        setTimeout(() => {
            iconEl.classList.remove('lf-marker-focus-pulse');
        }, 2500);
    }


    // Toggle Active Incidents Layer
    function toggleIncidentsLayer(visible) {
        if (!map || !incidentClusterGroup) return;
        if (visible) {
            if (!map.hasLayer(incidentClusterGroup)) map.addLayer(incidentClusterGroup);
        } else {
            if (map.hasLayer(incidentClusterGroup)) map.removeLayer(incidentClusterGroup);
        }
    }

    // Toggle Resolved Incidents Layer
    function toggleResolvedIncidentsLayer(visible) {
        if (!map || !resolvedClusterGroup) return;
        if (visible) {
            if (!map.hasLayer(resolvedClusterGroup)) map.addLayer(resolvedClusterGroup);
        } else {
            if (map.hasLayer(resolvedClusterGroup)) map.removeLayer(resolvedClusterGroup);
        }
    }

    function toggleTeamsLayer(visible) {
        if (!map || !teamLayerGroup) return;
        if (visible) {
            if (!map.hasLayer(teamLayerGroup)) map.addLayer(teamLayerGroup);
        } else {
            if (map.hasLayer(teamLayerGroup)) map.removeLayer(teamLayerGroup);
        }
    }

    function invalidateSize() {
        if (map) {
            setTimeout(() => map.invalidateSize(), 100);
        }
    }

    let _dotNetRef = null;

    function setDotNetRef(dotNetRef) {
        _dotNetRef = dotNetRef;
    }

    function onIncidentDetailClick(incidentId) {
        if (_dotNetRef) _dotNetRef.invokeMethodAsync('NotifyIncidentDetail', incidentId);
    }

    function onAssignTeamClick(incidentId) {
        if (_dotNetRef) _dotNetRef.invokeMethodAsync('NotifyAssignTeam', incidentId);
    }

    function destroyMap() {
        if (map) {
            map.remove();
            map = null;
            incidentMarkers = {};
            teamMarkers = {};
            incidentClusterGroup = null;
            resolvedClusterGroup = null;
            teamLayerGroup = null;
        }
    }

    function initPickerMap(containerId, initialLat, initialLng, initialZoom, dotNetRef, tileProvider) {
        if (pickerMap) {
            pickerMap.remove();
            pickerMap = null;
        }

        _pickerDotNetRef = dotNetRef;
        const lat = initialLat || 40.409264;
        const lng = initialLng || 49.867092;
        const zoom = initialZoom || 14;

        pickerMap = L.map(containerId, {
            center: [lat, lng],
            zoom: zoom,
            zoomControl: true,
            attributionControl: true
        });

        const tileUrl = getTileUrl(tileProvider);
        pickerTileLayer = L.tileLayer(tileUrl, {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(pickerMap);

        const pickerIcon = L.divIcon({
            html: `
              <svg xmlns="http://www.w3.org/2000/svg" width="34" height="42" viewBox="0 0 34 42">
                <path d="M17 0C7.6 0 0 7.6 0 17c0 12.8 17 25 17 25S34 29.8 34 17C34 7.6 26.4 0 17 0z" fill="#3b82f6" stroke="#ffffff" stroke-width="2"/>
                <circle cx="17" cy="17" r="6" fill="#ffffff"/>
              </svg>`,
            className: '',
            iconSize: [34, 42],
            iconAnchor: [17, 42]
        });

        pickerMarker = L.marker([lat, lng], {
            draggable: true,
            icon: pickerIcon
        }).addTo(pickerMap);

        pickerMap.on('click', function (e) {
            const clickedLat = e.latlng.lat;
            const clickedLng = e.latlng.lng;
            pickerMarker.setLatLng([clickedLat, clickedLng]);
            notifyPickerLocation(clickedLat, clickedLng);
        });

        pickerMarker.on('dragend', function () {
            const position = pickerMarker.getLatLng();
            notifyPickerLocation(position.lat, position.lng);
        });

        setTimeout(() => {
            if (pickerMap) pickerMap.invalidateSize();
        }, 200);
    }

    function notifyPickerLocation(lat, lng) {
        if (_pickerDotNetRef) {
            _pickerDotNetRef.invokeMethodAsync('NotifyLocationPicked', lat, lng);
        }
    }

    function setPickerLocation(lat, lng, zoom) {
        if (!pickerMap || !pickerMarker) return;
        pickerMarker.setLatLng([lat, lng]);
        pickerMap.setView([lat, lng], zoom || pickerMap.getZoom());
    }

    function updatePickerTileLayer(tileProvider) {
        if (!pickerMap || !pickerTileLayer) return;
        pickerMap.removeLayer(pickerTileLayer);
        pickerTileLayer = L.tileLayer(getTileUrl(tileProvider), { maxZoom: 19 }).addTo(pickerMap);
    }

    function getTileUrl(provider) {
        switch (provider) {
            case 'CartoDark':
                return 'https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Dark_Gray_Base/MapServer/tile/{z}/{y}/{x}';
            case 'CartoPositron':
                return 'https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Base/MapServer/tile/{z}/{y}/{x}';
            default:
                return 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
        }
    }

    function destroyPickerMap() {
        if (pickerMap) {
            pickerMap.remove();
            pickerMap = null;
            pickerMarker = null;
            _pickerDotNetRef = null;
            pickerTileLayer = null;
        }
    }

    function getCurrentBrowserLocation() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error("Geolocation is not supported by this browser."));
                return;
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                (error) => {
                    let message = "Unable to retrieve your location.";
                    if (error.code === error.PERMISSION_DENIED) {
                        message = "Location permission denied by user.";
                    } else if (error.code === error.POSITION_UNAVAILABLE) {
                        message = "Location information is unavailable.";
                    } else if (error.code === error.TIMEOUT) {
                        message = "The request to get user location timed out.";
                    }
                    reject(new Error(message));
                },
                { enableHighAccuracy: false, timeout: 6000, maximumAge: 60000 }
            );
        });
    }

    // Helper: Parse bounds from C# MapBoundsDto or array
    function parseBounds(customBounds) {
        if (!customBounds) return L.latLngBounds(SOCAR_FACILITY_BOUNDS);

        if (typeof customBounds === 'object' && customBounds.southWestLat !== undefined) {
            return L.latLngBounds(
                [customBounds.southWestLat, customBounds.southWestLng],
                [customBounds.northEastLat, customBounds.northEastLng]
            );
        }

        if (Array.isArray(customBounds) && customBounds.length === 2) {
            return L.latLngBounds(customBounds[0], customBounds[1]);
        }

        return L.latLngBounds(SOCAR_FACILITY_BOUNDS);
    }

    // Configure facility boundary lock with rigid elasticity
    function setMapBoundaryLock(enabled, customBounds) {
        const targetMaps = [map, pickerMap].filter(m => m !== null);
        if (targetMaps.length === 0) return;

        const bounds = parseBounds(customBounds);

        targetMaps.forEach(targetMap => {
            if (enabled) {
                targetMap.setMaxBounds(bounds);
                targetMap.options.maxBoundsViscosity = 1.0;
                targetMap.setMinZoom(DEFAULT_LOCKED_MIN_ZOOM);

                if (!bounds.contains(targetMap.getCenter())) {
                    targetMap.panInsideBounds(bounds, { animate: true });
                }
            } else {
                targetMap.setMaxBounds(null);
                targetMap.options.maxBoundsViscosity = 0.0;
                targetMap.setMinZoom(DEFAULT_UNLOCKED_MIN_ZOOM);
            }
            targetMap.invalidateSize();
        });
    }

    
    // Public API
    return {
        initMap,
        setMapBoundaryLock,
        updateMainTileLayer,
        addIncidentMarker,
        addTeamMarker,
        animateTeamMarker,
        updateIncidentStatus,
        updateTeamStatus,
        updateIncidentAssignment,
        removeIncidentMarker,
        removeTeamMarker,
        panToIncident,
        panToTeam,
        panToLocation,
        focusMarkerById,
        copyToClipboard,
        toggleIncidentsLayer,
        toggleResolvedIncidentsLayer,
        toggleTeamsLayer,
        invalidateSize,
        setDotNetRef,
        onIncidentDetailClick,
        onAssignTeamClick,
        destroyMap,
        initPickerMap,
        setPickerLocation,
        updatePickerTileLayer,
        destroyPickerMap,
        getCurrentBrowserLocation
    };

})();
