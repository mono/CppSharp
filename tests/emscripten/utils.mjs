export function assert(actual, expected, message) {
    if (arguments.length == 1)
        expected = true;

    if (actual === expected)
        return;

    if (actual !== null && expected !== null
        && typeof actual == 'object' && typeof expected == 'object'
        && actual.toString() === expected.toString())
        return;

    throw Error("assertion failed: got |" + actual + "|" +
        ", expected |" + expected + "|" +
        (message ? " (" + message + ")" : ""));
}

export const eq = assert;
export const floateq = (actual, expected) => { assert(Math.abs(actual - expected) < 0.01) }

export const ascii = v => v.charCodeAt(0)
