cmu-admin-ai-probe-description = Check the local AI worker through its Unix socket without generating a reply.
cmu-admin-ai-probe-help = Usage: admin_ai_probe. Requires Debug permission and cmu.admin_ai.enabled=true.
cmu-admin-ai-probe-disabled = AI health probe disabled or configuration invalid.
cmu-admin-ai-probe-busy = An AI health probe is already running.
cmu-admin-ai-probe-ready = Local AI worker health: Ready.
cmu-admin-ai-probe-unavailable = Local AI worker health: Unavailable.
cmu-admin-ai-status-description = Show cached AI worker, governor and queue status.
cmu-admin-ai-status-help = Usage: admin_ai_status
cmu-admin-ai-status = AI enabled={ $enabled }, state={ $state }, worker={ $health }, reason={ $reason }; pending={ $pending }, running={ $running }, accepted={ $accepted }, completed={ $completed }, rejected={ $rejected } (since reload).
cmu-admin-ai-status-load = Last measured load: CPU PSI={ $cpu }%, memory={ $memory }%, I/O={ $io }%; maximum tick boundary / budget={ $tick }.
cmu-admin-ai-draft-title = AI assistant — private draft
cmu-admin-ai-draft-prepare = Prepare
cmu-admin-ai-draft-regenerate = Retry
cmu-admin-ai-draft-cancel = Cancel
cmu-admin-ai-draft-insert = Insert
cmu-admin-ai-draft-sources = Sources
cmu-admin-ai-draft-private = Review the draft. Only you can send it to the player.
cmu-admin-ai-draft-worker = Worker: { $state }; waiting: { $pending }
cmu-admin-ai-draft-no-sources = No relevant local sources were found. Verify claims; the AI cannot confirm incident facts.
cmu-admin-ai-draft-status-disabled = AI is disabled by server configuration.
cmu-admin-ai-draft-status-unavailable = The local model is temporarily unavailable.
cmu-admin-ai-draft-status-paused = Generation is paused to protect game servers.
cmu-admin-ai-draft-status-noconversation = No current accepted conversation exists for this player.
cmu-admin-ai-draft-status-ready = A private draft can be prepared.
cmu-admin-ai-draft-status-working = The request is queued or running…
cmu-admin-ai-draft-status-draft = Draft ready. Edit and verify before sending.
cmu-admin-ai-draft-status-cancelled = Request cancelled.
cmu-admin-ai-draft-status-expired = Request expired. You can try again.
cmu-admin-ai-draft-status-denied = Insufficient permissions.
cmu-admin-ai-draft-status-invalidresponse = No valid draft was received. Nothing was sent.
cmu-admin-ai-draft-status-ratelimited = Too many requests. Wait a few seconds.
cmu-admin-ai-draft-status-queuefull = The queue is full. Try again later.
cmu-admin-ai-draft-status-stale = Context changed; the previous draft was cleared.
cmu-admin-ai-draft-status-knowledgeloading = Published local rules are loading. Try again in a few seconds.
