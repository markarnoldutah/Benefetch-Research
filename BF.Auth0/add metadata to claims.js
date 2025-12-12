  /**
 * Handler that will be called during the execution of a PostLogin flow.
 *
 * --- AUTH0 ACTIONS TEMPLATE https://github.com/auth0/opensource-marketplace/blob/main/templates/role-creation-POST_LOGIN ---
 *
 * @param {Event} event - Details about the user and the context in which they are logging in.
 * @param {PostLoginAPI} api - Interface whose methods can be used to change the behavior of the login.
 */
exports.onExecutePostLogin = async (event, api) => {

    const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
    const tenantIdClaim = 'http://benefetch.com/tenantId'
    const practiceIdsClaim = 'http://benefetch.com/practiceIds'

    let tenantId = findTenantId();
    let practiceIds = findPracticeIds();

    api.accessToken.setCustomClaim(tenantIdClaim, tenantId);
    api.accessToken.setCustomClaim(practiceIdsClaim, practiceIds);

    function findTenantId() {
        let tenantId = event.user.app_metadata.tenantId;
        if (tenantId) {
            return tenantId;
        } else {
            api.access.deny("No tenantId found in metadata.  You do not appear to be authorized for any tenants.");
        }
    }

    function findPracticeIds() {
        let practiceIds = event.user.app_metadata.practiceIds;
        if (practiceIds && Array.isArray(practiceIds) && practiceIds.length > 0) {
            return practiceIds;
        } else {
            api.access.deny("No practiceIds found in metadata.  You do not appear to be authorized for any practices.")
        }
    }
};

/**
 * Handler that will be invoked when this action is resuming after an external redirect. If your
 * onExecutePostLogin function does not perform a redirect, this function can be safely ignored.
 *
 * @param {Event} event - Details about the user and the context in which they are logging in.
 * @param {PostLoginAPI} api - Interface whose methods can be used to change the behavior of the login.
 */
// exports.onContinuePostLogin = async (event, api) => {
// };
