FROM quay.io/keycloak/keycloak:24.0

COPY realm-umbral.json /opt/keycloak/data/import/realm-umbral.json

CMD ["start-dev", "--import-realm", "--http-host=0.0.0.0", "--http-port=8080"]
