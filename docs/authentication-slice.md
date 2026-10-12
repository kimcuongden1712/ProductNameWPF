# Authentication vertical slice

This is an executable starter flow. The API payloads and profile fields below are assumptions and must be aligned with the real API contract before connecting to production.

## Assumed API contract

- `POST /signin` accepts JSON with `customerCode` and `password`.
- A successful sign-in returns JSON containing a non-empty `access_token`.
- `GET /me` accepts `Authorization: Bearer <token>` and returns:
  - `operator_id`
  - `display_name`
  - `company_name`
  - `branches`, an array with `id` and `name`
- HTTP 401 from sign-in means invalid credentials; HTTP 401 from `/me` means the token is invalid. A 403 is not treated as proof that the token expired.
- `POST /signout` is optional. It is not called unless `Api:SignOutPath` is configured.
- Dispatch access currently means that `/me` returned at least one branch.

Update the JSON property mappings in `AuthenticationApi` and the paths in `appsettings.json` when the actual API differs. Do not treat server-provided error bodies as safe user-facing messages without validating their content.

## Configuration

`src/ProductName.App.Wpf/appsettings.json` contains a deliberately non-routable placeholder API URL. Replace it with the correct HTTPS API base URL for the target environment. HTTP is permitted only for `localhost` by the Infrastructure registration.

The WPF project copies this file beside the executable. The Host content root is set to the application output directory so configuration does not depend on the process working directory.

## Session behavior

- The access token is encrypted with Windows DPAPI using `CurrentUser` and stored below the current user's Local Application Data directory.
- The token is persisted only after `/me` has returned the account profile successfully.
- At startup, a network/service failure preserves a saved token and keeps the application on the login screen with a retry action.
- An invalid token is cleared locally.
- If sign-in succeeds but `/me` is temporarily unavailable, the token remains only in memory as a pending profile request. Retry calls `/me` again without resending the password.
- Sign-out clears local session state and token before attempting the optional remote sign-out call.
- `AuthenticatedSessionHandler` is registered for the named `AuthenticatedApi` client. Business API adapters should use that client; it adds the current bearer token and invalidates the local session on HTTP 401. It is intentionally not used for `/signin` or `/me`.

## Project responsibilities

- `ProductName.AppCore`: authentication workflow, stable API/token-store contracts, account/session models.
- `ProductName.Infrastructure`: HTTP API adapter, response mapping, DPAPI token store, and registration extension.
- `ProductName.App.Wpf`: startup, login/work/dispatch UI, commands, and UI response to expired sessions.
- `ProductName.AppCore.Tests`: workflow tests using fakes; no real API, WPF, or DPAPI is required.

The dispatch view in this slice demonstrates the fixed MainView/SideView layout only. Replace those labels with real feature views as their workflows are implemented.
