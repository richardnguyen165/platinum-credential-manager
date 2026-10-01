# Credential search — where sorting and pagination live

> Design note for the Stage VI credential search (`GET /creds/search`). Records why sorting and
> pagination are done in the frontend, and the research behind that choice.

## Decision

| Concern | Where | Why |
|---|---|---|
| Filtering (category, service name, dates) | Backend | Only matching rows leave the database, and the per-user scoping check stays server-side. |
| Sorting | Frontend | The browser already holds every matching row, so re-sorting needs no request. |
| Pagination | Frontend | Same reason; it is a slice of the sorted array. |

The search endpoint returns **all** matching credentials for the logged-in user in one response.
[CredSearchup.vue](../PlatinumCredentialManager.Client/src/views/CredSearchup.vue) sorts and pages
that array locally.

Sorting and pagination must sit on the same side. "Page 2" only has a meaning once the rows are
ordered, so paginating in the backend while sorting in the frontend would sort only the rows on
the current page, not the full result set.

## Research: how many credentials does a user have?

Frontend sorting and pagination only work if the full result set is small enough to send at once.
Published figures put the average well under 1,000:

| Source | Year | Figure |
|---|---|---|
| NordPass | Feb 2020 | ~80 passwords per person |
| NordPass | Oct 2020 | ~100 passwords per person |
| NordPass | 2024 | 168 personal passwords, plus 87 for work |
| NordPass | 2025 | 120 passwords per person (drop attributed to single sign-on and passkeys) |
| Dashlane | 2023 | ~227–240 accounts per Dashlane user |
| Research cited by Newswire | — | 70–80 passwords for most people |

Dashlane's figure is the highest because it counts people who already use a password manager,
which is the closest match to this app's users.

**Caveats.** These are vendor studies based on surveys or the vendor's own user base, so they are
ballpark figures, not rigorous measurements. The Dashlane and NordPass 2025 numbers were taken
from search summaries of the linked pages and have not been checked against the original reports.

## Conclusion

A user is expected to hold tens to a few hundred credentials. Even at 1,000 rows the response is a
small JSON payload that the browser sorts instantly, so the simpler frontend approach is enough.

## When to revisit

Move sorting **and** pagination to the backend together if a single search could return thousands
of rows. That means adding `sortBy`, `page` and `pageSize` to `SearchCredentialDto`, applying
`OrderBy(...).Skip(...).Take(...)` before `ToListAsync()`, and returning a total count so the
frontend knows how many pages exist.

## Sources

- [NordPass — How many passwords does the average person have?](https://nordpass.com/blog/how-many-passwords-does-average-person-have/)
- [GlobeNewswire — People have around 170 passwords on average, study shows (2024)](https://www.globenewswire.com/news-release/2024/05/21/2885556/0/en/People-have-around-170-passwords-on-average-study-shows.HTML)
- [Dashlane — A Global Look at Password Health Scores in 2023](https://accounts.dashlane.com/resources/global-password-health-2023)
- [Newswire — New Research: Most People Have 70-80 Passwords](https://www.newswire.com/news/new-research-most-people-have-70-80-passwords-21103705)
- [Exploding Topics — 50+ Password Statistics](https://www.explodingtopics.com/blog/password-stats)
