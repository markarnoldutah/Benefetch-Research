/**
 * EC API – Collection-level Response Capture Tests
 * ------------------------------------------------
 * Automatically captures useful IDs from responses and stores them
 * as environment variables for chained workflows.
 *
 * Expected environment variables (some already used in pre-request script):
 *
 *  patientId
 *  coverageEnrollmentId          (vision coverage)
 *  medicalCoverageEnrollmentId   (medical coverage)
 *  encounterId                   (initial routine encounter)
 *  cobEncounterId                (dual coverage / COB scenario)
 *  medicalEncounterId            (medical visit)
 *  eligibilityCheckId
 *  visionPayerId
 *  medicalPayerId
 */

// ---------- Basic 2xx status check ----------
pm.test('Status code is 2xx', function () {
  pm.expect(pm.response.code).to.be.within(200, 299)
})

// ---------- Safely parse JSON ----------
let json
try {
  json = pm.response.json()
} catch (e) {
  console.log('Response is not JSON or body is empty; skipping capture logic.')
  return
}

const method = pm.request.method
const path = pm.request.url.path.join('/').toLowerCase()
const name = pm.request.name || ''

// Helpers
function isPlaceholder(val) {
  return !val || (typeof val === 'string' && val.startsWith('tmp-'))
}

function setIfEmptyOrPlaceholder(key, value) {
  if (!value) return
  const current = pm.environment.get(key)
  if (!current || isPlaceholder(current)) {
    pm.environment.set(key, value)
    console.log(`✅ Captured ${key} = ${value}`)
  } else {
    console.log(`ℹ️  ${key} already set (${current}), not overwriting.`)
  }
}

function getFirst(arr) {
  if (!Array.isArray(arr) || arr.length === 0) return undefined
  return arr[0]
}

// ---------- Capture logic by URL / DTO pattern ----------

// 1) PATIENTS
if (path.includes('patients')) {
  // Detail style: { patientId: "...", ... }
  if (json.patientId) {
    setIfEmptyOrPlaceholder('patientId', json.patientId)
  }

  // Paged search style: { items: [ { patientId: ... }, ... ] }
  if (Array.isArray(json.items)) {
    const first = getFirst(json.items)
    if (first && first.patientId) {
      setIfEmptyOrPlaceholder('patientId', first.patientId)
    }
  }
}

// 2) PAYERS
if (path.includes('payers') && method === 'POST') {
  // Payer DTO might look like: { payerId, name, type, ... }
  const payerId = json.payerId || json.id
  const type = (json.type || '').toLowerCase()
  if (payerId && type === 'vision') {
    setIfEmptyOrPlaceholder('visionPayerId', payerId)
  } else if (payerId && type === 'medical') {
    setIfEmptyOrPlaceholder('medicalPayerId', payerId)
  }
}

// 3) COVERAGE ENROLLMENTS
// /patients/{patientId}/coverage
if (path.includes('patients') && path.includes('coverage')) {
  // Coverage DTO: { coverageEnrollmentId, coverageType, ... }
  const covId = json.coverageEnrollmentId || json.id
  const covType = (json.coverageType || '').toLowerCase()

  if (covId && covType === 'vision') {
    setIfEmptyOrPlaceholder('coverageEnrollmentId', covId)
  } else if (covId && covType === 'medical') {
    setIfEmptyOrPlaceholder('medicalCoverageEnrollmentId', covId)
  }

  // For update calls that only return a subset, still try to capture
  if (!covType && covId) {
    // Fallback: just set generic coverageEnrollmentId if not already set
    setIfEmptyOrPlaceholder('coverageEnrollmentId', covId)
  }
}

// 4) ENCOUNTERS
if (path.includes('encounters') && method === 'POST') {
  // Encounter DTO: { encounterId, visitTypeCode, ... }
  const encId = json.encounterId || json.id
  const visitType = (json.visitTypeCode || '').toUpperCase()

  if (encId) {
    if (visitType === 'ROUTINE_EYE') {
      // First routine encounter becomes encounterId; next can be cobEncounterId
      if (isPlaceholder(pm.environment.get('encounterId'))) {
        setIfEmptyOrPlaceholder('encounterId', encId)
      } else {
        setIfEmptyOrPlaceholder('cobEncounterId', encId)
      }
    } else if (visitType === 'MEDICAL') {
      setIfEmptyOrPlaceholder('medicalEncounterId', encId)
    } else {
      // Unknown type – just set a generic encounterId if nothing else
      setIfEmptyOrPlaceholder('encounterId', encId)
    }
  }
}

// 5) COB ENDPOINT (if it returns encounter or decision object)
// Example (adjust if your DTO differs):
// { encounterId: "...", primaryCoverageEnrollmentId: "...", ... }
if (path.includes('encounters') && path.endsWith('/cob')) {
  if (json.encounterId) {
    setIfEmptyOrPlaceholder('cobEncounterId', json.encounterId)
  }
}

// 6) ELIGIBILITY CHECKS
if (path.includes('eligibility')) {
  // Single check DTO: { eligibilityCheckId, ... }
  const chkId = json.eligibilityCheckId || json.id

  if (chkId) {
    setIfEmptyOrPlaceholder('eligibilityCheckId', chkId)
  }

  // Search DTO: { items: [ { eligibilityCheckId, ... } ], ... }
  if (Array.isArray(json.items)) {
    const first = getFirst(json.items)
    if (first && (first.eligibilityCheckId || first.id)) {
      setIfEmptyOrPlaceholder('eligibilityCheckId', first.eligibilityCheckId || first.id)
    }
  }
}

// 7) LOG SUMMARY (optional, but handy)
console.log('🔎 EC capture summary:', {
  patientId: pm.environment.get('patientId'),
  coverageEnrollmentId: pm.environment.get('coverageEnrollmentId'),
  medicalCoverageEnrollmentId: pm.environment.get('medicalCoverageEnrollmentId'),
  encounterId: pm.environment.get('encounterId'),
  cobEncounterId: pm.environment.get('cobEncounterId'),
  medicalEncounterId: pm.environment.get('medicalEncounterId'),
  eligibilityCheckId: pm.environment.get('eligibilityCheckId'),
  visionPayerId: pm.environment.get('visionPayerId'),
  medicalPayerId: pm.environment.get('medicalPayerId'),
})
