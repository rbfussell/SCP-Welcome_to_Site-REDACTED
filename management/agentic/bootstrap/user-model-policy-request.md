# Actual user steering: model and reasoning policy

On November 11, 2026, after the actual turn.004 worker quota failure, the user asked:

> Continue :) let's be careful once we get all the basic setup going that we don't over use quota for excessive model hieight. CODE / DESIGN / ENGINEERING should be EXTRA HIGH model. the documentation should be low, the media should be Low. it works fine. The IA and Vince and so on, should be medium can we do this using GPT-6.1 sol?

This authorizes the exact model `gpt-6.1-sol` and explicit task-appropriate reasoning efforts (`xhigh`, `low`, `medium`) for subsequent dispatched agents. Earlier actual dispatches remain historical and are not relabelled. Ordinary coordination/review/evidence use medium; code/design/engineering use xhigh; documentation and media use low. This policy controls reasoning for media agents; the image-generation tool remains a separate tool.

Official configuration reference inspected: https://learn.chatgpt.com/docs/agent-configuration/subagents (retrieved November 11, 2026). Explicit spawn model/effort overrides defaults; inheriting a high-effort parent must not silently override the user-requested tiers.
