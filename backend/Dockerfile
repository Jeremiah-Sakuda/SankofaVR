# Optional question-and-story service. Nothing in this repository builds, pushes or runs this image.
#
# Build with this folder as the whole context, so the private competition brief, references and the
# Unity project can never enter the image:
#
#   docker build -t sankofa-voice backend
#
# See docs/release/voice-service-hosting.md before running it anywhere but a development machine.
FROM python:3.13-slim

ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
RUN useradd --system --no-create-home --shell /usr/sbin/nologin sankofa
WORKDIR /srv

# The service's own modules and the app's prepared tellings only. No credentials, .env file or documents.
COPY __init__.py ask.py content.py contracts.py providers.py server.py authored_tellings.json /srv/backend/

USER sankofa
EXPOSE 8787

# Plain HTTP for a TLS-terminating proxy or platform in front of it. Binding beyond loopback requires
# SANKOFA_SERVICE_TOKEN: without it the process exits instead of serving unauthenticated requests.
CMD ["python3", "-m", "backend.server", "--host", "0.0.0.0", "--port", "8787"]
