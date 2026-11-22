---
name: publish
description: Push main to GitHub after /submission-check passes, and confirm CI is green on Ubuntu and Windows. Human-triggered only.
disable-model-invocation: true
---
# /publish

1. Run `/submission-check`; stop on any failure.
2. Confirm `main` is clean, history is linear, and every commit is authored by the
   developer (`git log --format='%an <%ae>' | sort -u`).
3. `git push origin main` (the permission system asks the human to approve the push).
4. Watch the CI run (`gh run watch` on the latest run for `main`) and report the result and URL.
   If CI fails, fix through `/feature` or `/ship` and push again; never force-push.
