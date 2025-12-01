/**
 * EC API – Universal Pre-Request Script
 * -------------------------------------
 * Automatically manages environment variables used across workflows:
 *
 *  baseUrl
 *  accessToken
 *  patientId
 *  coverageEnrollmentId
 *  medicalCoverageEnrollmentId
 *  encounterId
 *  cobEncounterId
 *  medicalEncounterId
 *  eligibilityCheckId
 *  visionPayerId
 *  medicalPayerId
 *
 * This protects you from "undefined variable" errors
 * when running chained controller test flows.
 */

const requiredCoreVars = ['baseUrl', 'accessToken']
const optionalEntityVars = [
  'patientId',
  'coverageEnrollmentId',
  'medicalCoverageEnrollmentId',
  'encounterId',
  'cobEncounterId',
  'medicalEncounterId',
  'eligibilityCheckId',
  'visionPayerId',
  'medicalPayerId',
]

// ---------- Helper: check missing env vars ----------
function checkVariables() {
  let missing = []

  requiredCoreVars.forEach((v) => {
    if (!pm.environment.get(v)) missing.push(v)
  })

  if (missing.length > 0) {
    throw new Error(
      '❌ Required environment variables missing: ' +
        missing.join(', ') +
        '\n\nSet them before running EC workflows.'
    )
  }
}

// ---------- Helper: Auto-generate UUID-like placeholders ----------
function ensurePlaceholder(varName) {
  if (!pm.environment.get(varName)) {
    const uuid = 'tmp-' + Math.random().toString(36).substring(2, 12)
    pm.environment.set(varName, uuid)
    console.log(`⚠️  Auto-generated placeholder for ${varName}: ${uuid}`)
  }
}

// ---------- Execute checks ----------
checkVariables()

// For workflows where entities get created during the run,
// assign placeholder IDs so requests don’t break.
optionalEntityVars.forEach(ensurePlaceholder)

console.log('✅ EC Pre-Request Script executed.')
