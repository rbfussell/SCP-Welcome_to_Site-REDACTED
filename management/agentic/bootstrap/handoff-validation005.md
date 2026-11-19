# Actual structured-handoff validation — turn.005

KRA initial handoff was rejected with `Passing tests need actual log evidence`; worker preserved its initial JSON and corrected the unsupported qualified PASS to an explicit unqualified observation/NOT_RUN. No check was repeated. Corrected actual handoff was then recorded.

SWE initial handoff was rejected with `Artifact is outside the role-owned directory`; implemented shared source files were incorrectly listed as role-owned artifacts. The owner was instructed to preserve the initial JSON and move shared-source references to evidence without changing code or rerunning tests. QA authorization attempted before that correction was rejected with `Missing complete dependency handoff SWE-13`; no QA spawn or receipt was invented.

These validation rejections are interface corrections and do not erase actual source/test evidence or imply a production failure mutation.
