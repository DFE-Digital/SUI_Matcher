import './style.css'
import "govuk-frontend/dist/govuk/govuk-frontend.min.css";
import { initAll } from "govuk-frontend/dist/govuk/all.mjs";

initAll();

// Service navigation switching logic
const navPerson = document.getElementById('nav-person');
const navNhs = document.getElementById('nav-nhs');
const navLinkPerson = document.getElementById('nav-link-person');
const navLinkNhs = document.getElementById('nav-link-nhs');
const panelPerson = document.getElementById('panel-person');
const panelNhs = document.getElementById('panel-nhs');

function showPanel(panelToShow, navToActivate, navToDeactivate, otherPanel) {
    panelToShow.style.display = '';
    otherPanel.style.display = 'none';
    navToActivate.classList.add('govuk-header__navigation-item--active');
    navToDeactivate.classList.remove('govuk-header__navigation-item--active');
}

navLinkPerson.addEventListener('click', function(e) {
    e.preventDefault();
    showPanel(panelPerson, navPerson, navNhs, panelNhs);
});
navLinkNhs.addEventListener('click', function(e) {
    e.preventDefault();
    showPanel(panelNhs, navNhs, navPerson, panelPerson);
});

// Person form logic
const form = document.getElementById('personForm');
const responseDiv = document.getElementById('response');
const clearPersonBtn = document.getElementById('clearPersonForm');

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

form.addEventListener('submit', async function(event) {
    event.preventDefault();
    const given = document.getElementById('given').value;
    const family = document.getElementById('family').value;
    const birthdate = document.getElementById('birthdate').value;

    const data = { given, family, birthdate };

    try {
        const res = await fetch(`${API_BASE_URL}/matching/api/v1/matchperson`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await res.json();
        responseDiv.innerHTML = formatApiResponse(result);
    } catch (error) {
        responseDiv.textContent = 'Error: ' + error;
    }
});

clearPersonBtn.addEventListener('click', function() {
    form.reset();
    responseDiv.innerHTML = '';
});

// NHS number form logic
const nhsForm = document.getElementById('nhsForm');
const nhsResponseDiv = document.getElementById('nhs-response');
const clearNhsBtn = document.getElementById('clearNhsForm');
nhsForm.addEventListener('submit', async function(event) {
    event.preventDefault();
    const nhsNumber = document.getElementById('nhsNumberInput').value;
    try {
        const res = await fetch(`${API_BASE_URL}/matching/api/v1/demographics?nhsNumber=${nhsNumber}`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        });
        const resultObj = await res.json();
        let formatted;
        try {
            formatted = `<pre style='white-space:pre-wrap;word-break:break-word;' class='govuk-body'>${JSON.stringify(resultObj, null, 2)}</pre>`;
        } catch (e) {
            formatted = `<pre class='govuk-body'>${resultObj}</pre>`;
        }
        nhsResponseDiv.innerHTML = formatted;
    } catch (error) {
        nhsResponseDiv.textContent = 'Error: ' + error;
    }
});

clearNhsBtn.addEventListener('click', function() {
    nhsForm.reset();
    nhsResponseDiv.innerHTML = '';
});

function camelCaseToSentence(str) {
    return str.replace(/([a-z])([A-Z])/g, '$1 $2')
              .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2')
              .replace(/([0-9])([a-zA-Z])/g, '$1 $2')
              .replace(/([a-zA-Z])([0-9])/g, '$1 $2')
              .toLowerCase()
              .replace(/\b\w/g, char => char.toUpperCase());
}

function formatApiResponse(obj) {
  console.log(obj);
  if (!obj || !obj.result) return '<p class="govuk-body">No result data.</p>';

  const resultRows = [
    ['Match Status', obj.result.matchStatus || 'N/A'],
    ['NHS Number', obj.result.nhsNumber || 'N/A'],
  ].map(([label, value]) =>
    `<tr class="govuk-table__row"><td class="govuk-table__cell">${label}</td><td class="govuk-table__cell">${value}</td></tr>`
  ).join('');

  let html = `
    <table class="govuk-table">
      <caption class="govuk-table__caption govuk-table__caption--m">Result</caption>
      <thead class="govuk-table__head">
        <tr class="govuk-table__row">
          <th scope="col" class="govuk-table__header">Field</th>
          <th scope="col" class="govuk-table__header">Value</th>
        </tr>
      </thead>
      <tbody class="govuk-table__body">
        ${resultRows}
      </tbody>
    </table>
  `;

    if(obj.result.score >= 0.95){
        html += '<p class="govuk-inset-text">Confidence score is high at 95% and over.</p>';
    } else if (obj.result.score >= 85 && obj.result.score < 95) {
        html += '<p class="govuk-inset-text">Confidence score is moderate at 85% to 95%, indicating only a potential match.</p>';
    } else {
        html += '<p class="govuk-inset-text">Confidence score is low at below 85%.</p>';
    }

  if (obj.dataQuality) {
    const dqRows = Object.entries(obj.dataQuality).map(
      ([key, value]) =>
        `<tr class="govuk-table__row"><td class="govuk-table__cell">${camelCaseToSentence(key)}</td><td class="govuk-table__cell">${value}</td></tr>`
    ).join('');
    html += `
      <h2 class="govuk-heading-m">Data Quality</h2>
      <table class="govuk-table">
        <thead class="govuk-table__head">
          <tr class="govuk-table__row">
            <th scope="col" class="govuk-table__header">Field</th>
            <th scope="col" class="govuk-table__header">Status</th>
          </tr>
        </thead>
        <tbody class="govuk-table__body">
          ${dqRows}
        </tbody>
      </table>
    `;
  }
  return html;
}
