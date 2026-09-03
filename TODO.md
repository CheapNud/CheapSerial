<!--
  TODO.md — CheapSerial project work tracker
  Last updated: 2026-09-03 (three Planned items: rich enumeration, line-framing reads, write-and-await-predicate)

  RULES FOR AI AGENTS:
  - Update the "Last updated" date above whenever you modify this file
  - Items use checkbox format: - [ ] incomplete, - [x] complete
  - Never remove completed items — they serve as history. Move them to "## Done" when a category gets cluttered.
  - Each item gets ONE line. Details go in sub-bullets indented with 2 spaces.
  - Prefix each item with the date it was added: - [ ] (2026-03-17) Description
  - When completing, change to: - [x] (2026-03-17 → 2026-03-18) Description
  - Tag the SOURCE of each item at the end in brackets:
      [code-todo] = from // TODO comment in source code
      [plan] = from a plan document or planning session
      [bug] = from a bug encountered during dev/deploy
      [audit] = from a code audit or review
      [user] = explicitly requested by the user
  - For [code-todo] items, ALWAYS include file:line reference so devs can navigation directly
  - Categories: Blocking, Planned, Future, Done
  - New items go at the TOP of their category
  - Do not create separate TODO_*.md files — everything goes here
  - Keep it terse. If it needs more than 3 sub-bullets, link to a plan document.
  - Do NOT create, rename, or remove categories — the fixed set is: Blocking, Planned, Future, Done
  - When asked for planned work or TODO analysis, ALWAYS include Future items too — list them below Planned and note them as future work
-->

# TODO

## Blocking

_Nothing blocking._

## Planned

- [ ] (2026-09-03) Rich port enumeration: descriptions + blacklist, not just `GetPortNames()` [user]
  - WMI `Win32_PnPEntity` filtered on the Ports class GUID gives (PortName, Description, DeviceId) → enables `FindPortByDescription("CP210x")`, a never-a-device blacklist (Intel AMT/ME, Bluetooth SPP, virtual/VM COM ports), and a `GetDiagnostics()` dump for callers
  - Description whitelists must be ARRAYS: the same USB-serial adapter's friendly name differs per Windows UI language ("USB Serial Port" / "Serieel USB-apparaat" / "USB-Seriell" / "Périphérique série USB")
  - `System.Management` is Windows-only — gate it; keep `GetPortNames()` as the portable path. Ambiguity rule: two identical chipsets ⇒ refuse to auto-assign, never guess
- [ ] (2026-09-03) `ReadLinesAsync(CancellationToken) : IAsyncEnumerable<string>` line-framing read mode [user]
  - Backed by an unbounded `Channel<string>` (`SingleReader = true`); the DataReceived handler only buffers and pushes complete lines — all consumer work happens on the reading task, off the port's event thread
  - Avoid the O(n²) trap: index into the buffer with an offset, don't `sb.ToString().IndexOf('\n')` per iteration (or use System.IO.Pipelines)
- [ ] (2026-09-03) `WriteAndWaitForAsync(payload, predicate, timeout, ct)` — write, complete on first matching response [user]
  - Fresh `TaskCompletionSource` per call (a shared one races between overlapping calls), raced against the timeout, linked to the cancel token
  - Optional caller-side heuristic worth documenting: N consecutive IDENTICAL bad responses ⇒ abort early, the peer is wedged

## Future

_Nothing in future._

## Done

_Nothing done yet._
