## Project tracking — GitHub Issues are the source of truth

This repo tracks all work in GitHub Issues. Before starting any task, read the
relevant issue; if none exists, create one. Status lives on the project board,
not in chat or code.

- Hierarchy: feature = parent issue (`type: feature`); tasks/sub-bugs = native
  sub-issues. This machine's gh predates native `--parent`, so create them with
  `gh sub-issue create --parent <n>` (gh-sub-issue extension); on gh 2.94.0+ use
  `gh issue create --parent <n>`. Branch with `gh issue develop <n> --checkout`.
- Labels: one `type:` (feature|bug|task|chore), one `priority:`, one `area:`
  (core|ui|overlay|config|build|docs).
- Issue bodies use Description / Acceptance criteria (testable checkboxes) /
  Non-goals, written in AWS docs style: active voice, present tense, second
  person, concise, sentence-case headings, no "please/simply/just".
- On starting: comment the plan, move the card to In Progress. On finishing:
  comment implementation notes and open a PR with `Fixes #<n>`.
- Log discovered work as new issues. Never leave a code-only TODO.
