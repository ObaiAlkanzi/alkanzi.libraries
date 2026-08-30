/* ============================================================
   erpApi — the single door to Alkanzi.Erp.Api.

   Every call the front end makes to the API goes through here, so
   the base URL, the bearer token and the handling of an expired
   session are decided once instead of at each call site.

   The token is issued at sign-in and rendered into the page by
   _workspaceShell. It is short-lived by design, so a 401 is an
   expected outcome rather than an error: it means the session
   needs renewing, and the only way to renew is to sign in again.
   ============================================================ */
app.factory("erpApi", ["$http", "$q", "$window", function ($http, $q, $window) {
    "use strict";

    var config = $window.__ERP__ || {};
    var baseUrl = (config.apiBaseUrl || "").replace(/\/+$/, "");
    var token = config.token || "";

    function url(path) {
        return baseUrl + (path.charAt(0) === "/" ? path : "/" + path);
    }

    function headers() {
        // No Authorization header at all when there is no token, rather than "Bearer ".
        // An empty bearer is a malformed credential and reads as a broken client; its absence
        // reads as an anonymous call, which is what it is.
        return token ? { Authorization: "Bearer " + token } : {};
    }

    function handle(promise) {
        return promise.then(
            function (response) { return response.data; },
            function (response) {
                if (response.status === 401) {
                    // The cookie session may still be alive while the API token has expired,
                    // which looks like the app half-working. Send the user to sign in again,
                    // preserving where they were.
                    $window.location.href = "/Account/Login?ReturnUrl=" +
                        encodeURIComponent($window.location.pathname + $window.location.search);
                    return $q.reject(response);
                }

                if (response.status === 403) {
                    DevExpress.ui.notify("You do not have permission to do that.", "error", 4000);
                    return $q.reject(response);
                }

                // The API returns { error, message } for anything it refuses deliberately;
                // surface that message rather than a generic failure, because it usually says
                // exactly what is wrong ("this company still has branches").
                var data = response.data || {};
                if (data.message) DevExpress.ui.notify(data.message, "error", 5000);
                else if (response.status === -1) DevExpress.ui.notify("Cannot reach the API. Is it running?", "error", 5000);
                else DevExpress.ui.notify("Request failed (" + response.status + ").", "error", 4000);

                return $q.reject(response);
            });
    }

    return {
        hasToken: function () { return !!token; },
        baseUrl: baseUrl,

        get: function (path, params) { return handle($http.get(url(path), { params: params || {}, headers: headers() })); },
        post: function (path, body) { return handle($http.post(url(path), body, { headers: headers() })); },
        put: function (path, body) { return handle($http.put(url(path), body, { headers: headers() })); },
        del: function (path) { return handle($http.delete(url(path), { headers: headers() })); }
    };
}]);
