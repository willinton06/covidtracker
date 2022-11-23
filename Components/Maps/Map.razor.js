let _map;
let _statesByName = [];
let _lastSelectedState;
const localStorageGeoJsonKey = 'states-geoJson';

export async function init(state) {
    let states = localStorage.getItem(localStorageGeoJsonKey);

    if (!states) {
        // This is a ton of data so we want to cache this too
        states = await (await fetch('_content/CovidTracker.Components/assets/states-geoJson.json')).json();
        localStorage.setItem(localStorageGeoJsonKey, JSON.stringify(states));
    }
    else states = JSON.parse(states);

    _map = L.map('map');

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(_map);

    L.geoJson(states, {
        onEachFeature: onStateAdded
    }).addTo(_map);

    panTo(state);
}

/* We highlight whichever state the user clicks just to keep it as clear as possible */

export function panTo(state) {
    _lastSelectedState?.setStyle({ color: '#3388ff' })

    _lastSelectedState = _statesByName[state];

    if (!_lastSelectedState) return;

    _lastSelectedState.setStyle({ color: '#5eba7d' });

    _map.fitBounds(_lastSelectedState.getBounds());
}

function onStateAdded(feature, layer) {
    _statesByName[feature.properties.NAME] = layer;
}