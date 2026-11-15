/**
 * Prüft die unterstützten JSON-Schema-2020-12-Schlüsselwörter der Telemetrie-Schemata.
 * Die Schema-Dokumente werden erzeugt. Diese Datei implementiert ihre Auswertung.
 */

type JsonSchemaNode = boolean | Record<string, unknown>;

const telemetryCheckpointSupportedSchemaKeywords = new Set([
    "$schema", "$id", "$defs", "$ref", "title", "description", "allOf", "anyOf", "oneOf",
    "not", "const", "enum", "type", "minLength", "maxLength", "pattern", "format", "minimum",
    "maximum", "minItems", "maxItems", "items", "required", "properties", "additionalProperties",
    "unevaluatedProperties", "if", "then", "else"
]);
const telemetryCheckpointSupportedSchemaTypes = new Set([
    "null", "boolean", "string", "number", "integer", "array", "object"
]);

const validateTelemetryCheckpointEnvelope = (value: unknown): boolean => {
    const root = telemetryCheckpointSchemaDocuments[telemetryCheckpointRootSchemaId];
    return validateTelemetrySchemaNode(root as JsonSchemaNode, value, telemetryCheckpointRootSchemaId);
};

const validateTelemetrySchemaNode = (
    schema: JsonSchemaNode,
    value: unknown,
    documentId: string
): boolean => {
    if (schema === true) {
        return true;
    }
    if (schema === false || !schema || typeof schema !== "object" || Array.isArray(schema)) {
        return false;
    }
    if (!telemetrySchemaOperandsAreValid(schema)) {
        return false;
    }

    const reference = schema.$ref;
    if (typeof reference === "string") {
        const resolved = resolveTelemetrySchemaReference(reference, documentId);
        if (resolved === null
            || !validateTelemetrySchemaNode(resolved.schema, value, resolved.documentId)) {
            return false;
        }
    }

    const allOf = schema.allOf;
    if (Array.isArray(allOf)
        && !allOf.every((candidate) => validateTelemetrySchemaNode(candidate as JsonSchemaNode, value, documentId))) {
        return false;
    }
    const anyOf = schema.anyOf;
    if (Array.isArray(anyOf)
        && !anyOf.some((candidate) => validateTelemetrySchemaNode(candidate as JsonSchemaNode, value, documentId))) {
        return false;
    }
    const oneOf = schema.oneOf;
    if (Array.isArray(oneOf)
        && oneOf.filter((candidate) =>
            validateTelemetrySchemaNode(candidate as JsonSchemaNode, value, documentId)).length !== 1) {
        return false;
    }
    if (schema.not !== undefined
        && validateTelemetrySchemaNode(schema.not as JsonSchemaNode, value, documentId)) {
        return false;
    }

    if (schema.if !== undefined) {
        const ifMatched = validateTelemetrySchemaNode(schema.if as JsonSchemaNode, value, documentId);
        if (ifMatched) {
            if (schema.then !== undefined
                && !validateTelemetrySchemaNode(schema.then as JsonSchemaNode, value, documentId)) {
                return false;
            }
        } else if (schema.else !== undefined
            && !validateTelemetrySchemaNode(schema.else as JsonSchemaNode, value, documentId)) {
            return false;
        }
    }

    if (schema.const !== undefined && !jsonValuesEqual(value, schema.const)) {
        return false;
    }
    if (Array.isArray(schema.enum)
        && !schema.enum.some((candidate) => jsonValuesEqual(value, candidate))) {
        return false;
    }

    const declaredTypes = typeof schema.type === "string"
        ? [schema.type]
        : Array.isArray(schema.type) ? schema.type : [];
    if (declaredTypes.length > 0 && !declaredTypes.some((type) => jsonValueHasType(value, type))) {
        return false;
    }

    if (typeof value === "string") {
        if (typeof schema.minLength === "number" && value.length < schema.minLength) return false;
        if (typeof schema.maxLength === "number" && value.length > schema.maxLength) return false;
        if (typeof schema.pattern === "string" && !(new RegExp(schema.pattern, "u")).test(value)) return false;
        if (schema.format === "date-time"
            && (Number.isNaN(Date.parse(value)) || new Date(value).toISOString() !== value)) return false;
    }

    if (typeof value === "number") {
        if (typeof schema.minimum === "number" && value < schema.minimum) return false;
        if (typeof schema.maximum === "number" && value > schema.maximum) return false;
    }

    if (Array.isArray(value)) {
        if (typeof schema.minItems === "number" && value.length < schema.minItems) return false;
        if (typeof schema.maxItems === "number" && value.length > schema.maxItems) return false;
        if (schema.items !== undefined
            && !value.every((item) => validateTelemetrySchemaNode(schema.items as JsonSchemaNode, item, documentId))) {
            return false;
        }
    }

    if (value !== null && typeof value === "object" && !Array.isArray(value)) {
        const candidate = value as Record<string, unknown>;
        const required = Array.isArray(schema.required) ? schema.required : [];
        if (!required.every((key) => typeof key === "string"
            && Object.prototype.hasOwnProperty.call(candidate, key))) {
            return false;
        }

        const properties = schema.properties !== null
            && typeof schema.properties === "object"
            && !Array.isArray(schema.properties)
            ? schema.properties as Record<string, JsonSchemaNode>
            : {};
        for (const [key, propertyValue] of Object.entries(candidate)) {
            const propertySchema = properties[key];
            if (propertySchema !== undefined) {
                if (!validateTelemetrySchemaNode(propertySchema, propertyValue, documentId)) return false;
            } else if (schema.additionalProperties === false) {
                return false;
            } else if (schema.additionalProperties !== undefined
                && schema.additionalProperties !== true
                && !validateTelemetrySchemaNode(
                    schema.additionalProperties as JsonSchemaNode,
                    propertyValue,
                    documentId)) {
                return false;
            }
        }

        if (schema.unevaluatedProperties !== undefined) {
            const evaluatedProperties = collectTelemetryEvaluatedProperties(
                schema,
                candidate,
                documentId,
                false);
            if (evaluatedProperties === null) {
                return false;
            }
            for (const [key, propertyValue] of Object.entries(candidate)) {
                if (evaluatedProperties.has(key)) {
                    continue;
                }
                if (schema.unevaluatedProperties === false) {
                    return false;
                }
                if (schema.unevaluatedProperties !== true
                    && !validateTelemetrySchemaNode(
                        schema.unevaluatedProperties as JsonSchemaNode,
                        propertyValue,
                        documentId)) {
                    return false;
                }
            }
        }
    }

    return true;
};

// JSON Schema 2020-12 wendet `unevaluatedProperties` an, nachdem Annotationen aus erfolgreichen Referenzen und Applikatoren gesammelt wurden.
// additionalProperties berücksichtigt dagegen nur die im selben Schemaobjekt deklarierten Eigenschaften,
// während `unevaluatedProperties` ein zusammengesetztes `allOf` schließt, nachdem jeder Zweig beigetragen hat.
const collectTelemetryEvaluatedProperties = (
    schema: JsonSchemaNode,
    value: Record<string, unknown>,
    documentId: string,
    includeOwnUnevaluatedProperties = true
): Set<string> | null => {
    if (typeof schema === "boolean") {
        return new Set<string>();
    }
    if (!schema || typeof schema !== "object" || Array.isArray(schema)) {
        return null;
    }

    const evaluated = new Set<string>();
    const merge = (properties: Set<string> | null): boolean => {
        if (properties === null) {
            return false;
        }
        properties.forEach((property) => evaluated.add(property));
        return true;
    };
    const collect = (candidate: JsonSchemaNode, candidateDocumentId = documentId): boolean =>
        merge(collectTelemetryEvaluatedProperties(candidate, value, candidateDocumentId));

    if (typeof schema.$ref === "string") {
        const resolved = resolveTelemetrySchemaReference(schema.$ref, documentId);
        if (resolved === null || !collect(resolved.schema, resolved.documentId)) {
            return null;
        }
    }
    if (Array.isArray(schema.allOf)) {
        for (const candidate of schema.allOf) {
            if (!collect(candidate as JsonSchemaNode)) {
                return null;
            }
        }
    }
    if (Array.isArray(schema.anyOf)) {
        for (const candidate of schema.anyOf) {
            if (validateTelemetrySchemaNode(candidate as JsonSchemaNode, value, documentId)
                && !collect(candidate as JsonSchemaNode)) {
                return null;
            }
        }
    }
    if (Array.isArray(schema.oneOf)) {
        for (const candidate of schema.oneOf) {
            if (validateTelemetrySchemaNode(candidate as JsonSchemaNode, value, documentId)
                && !collect(candidate as JsonSchemaNode)) {
                return null;
            }
        }
    }
    if (schema.if !== undefined) {
        const ifMatched = validateTelemetrySchemaNode(schema.if as JsonSchemaNode, value, documentId);
        if (ifMatched) {
            if (schema.then !== undefined && !collect(schema.then as JsonSchemaNode)) {
                return null;
            }
        } else if (schema.else !== undefined && !collect(schema.else as JsonSchemaNode)) {
            return null;
        }
    }

    const properties = schema.properties !== null
        && typeof schema.properties === "object"
        && !Array.isArray(schema.properties)
        ? schema.properties as Record<string, JsonSchemaNode>
        : {};
    Object.keys(properties).forEach((property) => {
        if (Object.prototype.hasOwnProperty.call(value, property)) {
            evaluated.add(property);
        }
    });

    if (schema.additionalProperties !== undefined) {
        Object.keys(value).forEach((property) => {
            if (!Object.prototype.hasOwnProperty.call(properties, property)) {
                evaluated.add(property);
            }
        });
    }

    if (includeOwnUnevaluatedProperties && schema.unevaluatedProperties !== undefined) {
        Object.keys(value).forEach((property) => evaluated.add(property));
    }
    return evaluated;
};

// Auch erzeugte Schemata werden vor der Auswertung geprüft. Ein fehlerhafter Operand
// darf nicht wie ein fehlendes Schlüsselwort behandelt werden und die Prüfung umgehen.
const telemetrySchemaOperandsAreValid = (
    schema: JsonSchemaNode,
    ancestors: Set<Record<string, unknown>> = new Set()
): boolean => {
    if (typeof schema === "boolean") {
        return true;
    }
    if (!schema || typeof schema !== "object" || Array.isArray(schema) || ancestors.has(schema)) {
        return false;
    }

    const nextAncestors = new Set(ancestors);
    nextAncestors.add(schema);
    const isSchema = (value: unknown): boolean =>
        (typeof value === "boolean"
            || (value !== null && typeof value === "object" && !Array.isArray(value)))
        && telemetrySchemaOperandsAreValid(value as JsonSchemaNode, nextAncestors);
    const isSchemaMap = (value: unknown): boolean =>
        value !== null
        && typeof value === "object"
        && !Array.isArray(value)
        && Object.values(value as Record<string, unknown>).every(isSchema);
    const isNonNegativeInteger = (value: unknown): boolean =>
        typeof value === "number" && Number.isInteger(value) && value >= 0;
    const isFiniteNumber = (value: unknown): boolean =>
        typeof value === "number" && Number.isFinite(value);
    const isUniqueStringArray = (value: unknown, allowEmpty: boolean): boolean =>
        Array.isArray(value)
        && (allowEmpty || value.length > 0)
        && value.every((item) => typeof item === "string")
        && new Set(value).size === value.length;

    for (const [keyword, operand] of Object.entries(schema)) {
        if (!telemetryCheckpointSupportedSchemaKeywords.has(keyword)) {
            return false;
        }
        switch (keyword) {
            case "$schema":
            case "$id":
            case "$ref":
            case "title":
            case "description":
                if (typeof operand !== "string") return false;
                break;
            case "pattern":
                if (typeof operand !== "string") return false;
                try {
                    new RegExp(operand, "u");
                } catch {
                    return false;
                }
                break;
            case "format":
                if (operand !== "date-time") return false;
                break;
            case "$defs":
            case "properties":
                if (!isSchemaMap(operand)) return false;
                break;
            case "allOf":
            case "anyOf":
            case "oneOf":
                if (!Array.isArray(operand) || operand.length === 0 || !operand.every(isSchema)) return false;
                break;
            case "not":
            case "items":
            case "if":
            case "then":
            case "else":
                if (!isSchema(operand)) return false;
                break;
            case "additionalProperties":
            case "unevaluatedProperties":
                if (!isSchema(operand)) return false;
                break;
            case "enum":
                if (!Array.isArray(operand)
                    || operand.length === 0
                    || !operand.every(isJsonValue)
                    || new Set(operand.map((item) => JSON.stringify(item))).size !== operand.length) return false;
                break;
            case "const":
                if (!isJsonValue(operand)) return false;
                break;
            case "type": {
                const types = typeof operand === "string" ? [operand] : operand;
                if (!isUniqueStringArray(types, false)
                    || !(types as string[]).every((type) => telemetryCheckpointSupportedSchemaTypes.has(type))) return false;
                break;
            }
            case "minLength":
            case "maxLength":
            case "minItems":
            case "maxItems":
                if (!isNonNegativeInteger(operand)) return false;
                break;
            case "minimum":
            case "maximum":
                if (!isFiniteNumber(operand)) return false;
                break;
            case "required":
                if (!isUniqueStringArray(operand, true)) return false;
                break;
        }
    }
    return true;
};

const isJsonValue = (value: unknown): boolean => {
    if (value === null || typeof value === "string" || typeof value === "boolean") return true;
    if (typeof value === "number") return Number.isFinite(value);
    if (Array.isArray(value)) return value.every(isJsonValue);
    return typeof value === "object"
        && Object.values(value as Record<string, unknown>).every(isJsonValue);
};

const resolveTelemetrySchemaReference = (
    reference: string,
    currentDocumentId: string
): { schema: JsonSchemaNode; documentId: string } | null => {
    const hashIndex = reference.indexOf("#");
    const referencedDocumentId = hashIndex < 0
        ? reference
        : reference.slice(0, hashIndex) || currentDocumentId;
    const pointer = hashIndex < 0 ? "" : reference.slice(hashIndex + 1);
    let current = telemetryCheckpointSchemaDocuments[referencedDocumentId];
    if (current === undefined) {
        return null;
    }
    if (pointer.length > 0) {
        if (!pointer.startsWith("/")) return null;
        for (const rawSegment of pointer.slice(1).split("/")) {
            const segment = decodeURIComponent(rawSegment).replace(/~1/g, "/").replace(/~0/g, "~");
            if (!current || typeof current !== "object" || Array.isArray(current)) return null;
            current = (current as Record<string, unknown>)[segment];
        }
    }
    if (typeof current !== "boolean"
        && (!current || typeof current !== "object" || Array.isArray(current))) {
        return null;
    }
    return { schema: current as JsonSchemaNode, documentId: referencedDocumentId };
};

const jsonValueHasType = (value: unknown, type: unknown): boolean => {
    switch (type) {
        case "null": return value === null;
        case "boolean": return typeof value === "boolean";
        case "string": return typeof value === "string";
        case "number": return typeof value === "number" && Number.isFinite(value);
        case "integer": return typeof value === "number" && Number.isInteger(value);
        case "array": return Array.isArray(value);
        case "object": return value !== null && typeof value === "object" && !Array.isArray(value);
        default: return false;
    }
};

const jsonValuesEqual = (left: unknown, right: unknown): boolean =>
    JSON.stringify(left) === JSON.stringify(right);
