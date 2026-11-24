#!/usr/bin/env bash
# Two-way sync of branches and tags between two git remotes ("origin" = Forgejo,
# "github" = GitHub). Must be run inside a git repository that has both remotes.
#
# Per branch:
#   - only on one side          -> created on the other side
#   - identical                 -> nothing to do
#   - one side is ahead         -> fast-forwarded on the other side
#   - both sides have new commits (diverged) -> left alone, reported, exit code 1
#
# Branches listed in SYNC_EXCLUDE (space-separated, shell globs allowed) are skipped,
# e.g. generated branches like gh-pages that are force-pushed by a deploy job.
#
# Branch deletions are NOT propagated (a branch deleted on only one side is
# recreated from the other). Delete branches on both sides.
# Tags are only created, never moved or deleted.
set -uo pipefail

A=${SYNC_REMOTE_A:-origin}
B=${SYNC_REMOTE_B:-github}
EXCLUDE=${SYNC_EXCLUDE:-}

is_excluded() {
  local pattern
  for pattern in $EXCLUDE; do
    # shellcheck disable=SC2053
    [[ "$1" == $pattern ]] && return 0
  done
  return 1
}

git fetch --prune --no-tags "$A" "+refs/heads/*:refs/remotes/$A/*" || exit 2
git fetch --prune --no-tags "$B" "+refs/heads/*:refs/remotes/$B/*" || exit 2

branches=$( { git for-each-ref --format='%(refname:lstrip=3)' "refs/remotes/$A/";
              git for-each-ref --format='%(refname:lstrip=3)' "refs/remotes/$B/"; } \
            | grep -vx 'HEAD' | sort -u )

conflicts=()
failures=()

push() { # remote sha branch
  if git push "$1" "$2:refs/heads/$3"; then
    echo "  -> pushed to $1"
  else
    failures+=("$3 -> $1")
  fi
}

for br in $branches; do
  if is_excluded "$br"; then
    echo "[$br] excluded"
    continue
  fi

  a=$(git rev-parse -q --verify "refs/remotes/$A/$br" || true)
  b=$(git rev-parse -q --verify "refs/remotes/$B/$br" || true)

  if [[ "$a" == "$b" ]]; then
    continue
  elif [[ -z "$b" ]]; then
    echo "[$br] only on $A"; push "$B" "$a" "$br"
  elif [[ -z "$a" ]]; then
    echo "[$br] only on $B"; push "$A" "$b" "$br"
  elif git merge-base --is-ancestor "$b" "$a"; then
    echo "[$br] $A is ahead"; push "$B" "$a" "$br"
  elif git merge-base --is-ancestor "$a" "$b"; then
    echo "[$br] $B is ahead"; push "$A" "$b" "$br"
  else
    echo "[$br] DIVERGED: $A=${a:0:8} $B=${b:0:8} - resolve by hand"
    conflicts+=("$br")
  fi
done

# Tags: create missing ones on each side, never overwrite.
git fetch --no-tags "$A" "+refs/tags/*:refs/sync-tags/$A/*" >/dev/null 2>&1 || true
git fetch --no-tags "$B" "+refs/tags/*:refs/sync-tags/$B/*" >/dev/null 2>&1 || true
for side in "$A:$B" "$B:$A"; do
  from=${side%%:*}; to=${side##*:}
  for tag in $(git for-each-ref --format='%(refname:lstrip=3)' "refs/sync-tags/$from/"); do
    if ! git rev-parse -q --verify "refs/sync-tags/$to/$tag" >/dev/null; then
      echo "[tag $tag] only on $from"
      git push "$to" "refs/sync-tags/$from/$tag:refs/tags/$tag" || failures+=("tag $tag -> $to")
    fi
  done
done

if (( ${#conflicts[@]} || ${#failures[@]} )); then
  (( ${#conflicts[@]} )) && echo "Diverged branches (not synced): ${conflicts[*]}"
  (( ${#failures[@]} ))  && echo "Failed pushes: ${failures[*]}"
  exit 1
fi
echo "Sync complete."
