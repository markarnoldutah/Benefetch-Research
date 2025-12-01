let json = pm.response.json();
pm.environment.set("accessToken", json.access_token);
console.log("🔑 Stored Auth0 token:", json.access_token);
