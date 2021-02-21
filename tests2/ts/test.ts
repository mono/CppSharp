import * as test from "tests";


function builtins()
{
    eq(test.ReturnsVoid(), undefined)

    eq(test.ReturnsBool(), true)
    eq(test.PassAndReturnsBool(false), false)

    eq(test.ReturnsNullptr(), null)
    //eq(test.PassAndReturnsNullptr(null), null)
    eq(test.ReturnsNullptr(), null)

    eq(test.ReturnsChar (), ascii('a'));
    eq(test.ReturnsSChar(), ascii('a'));
    eq(test.ReturnsUChar(), ascii('a'));

    eq(test.PassAndReturnsChar (ascii('a')), ascii('a'));
    eq(test.PassAndReturnsSChar(ascii('b')), ascii('b'));
    eq(test.PassAndReturnsUChar(ascii('c')), ascii('c'));

    // TODO: add wchar_t tests

    eq(test.ReturnsFloat (), 5.0);
    eq(test.ReturnsDouble(), -5.0);
    //eq(test.ReturnsLongDouble(), -5.0);

    //floateq(test.PassAndReturnsFloat (1.32), 1.32);
    floateq(test.PassAndReturnsDouble(1.32), 1.32);
    //float(test.PassAndReturnsLongDouble(1.32), 1.32);

    eq(test.ReturnsInt8  (), -5);
    eq(test.ReturnsUInt8 (),  5);
    eq(test.ReturnsInt16 (), -5);
    eq(test.ReturnsUInt16(),  5);
    eq(test.ReturnsInt32 (), -5);
    eq(test.ReturnsUInt32(),  5);
    eq(test.ReturnsInt64 (), -5n);
    eq(test.ReturnsUInt64(),  5n);

    const int8 = { min: -(2**7), max: (2**7) - 1 };
    eq(test.PassAndReturnsInt8(int8.min), int8.min);
    eq(test.PassAndReturnsInt8(int8.max), int8.max);

    const uint8 = { min: 0, max: (2**8) - 1 };
    eq(test.PassAndReturnsUInt8(uint8.min), uint8.min);
    eq(test.PassAndReturnsUInt8(uint8.max), uint8.max);

    const int16 = { min: -(2**15), max: (2**15) - 1 };
    eq(test.PassAndReturnsInt16(int16.min), int16.min);
    eq(test.PassAndReturnsInt16(int16.max), int16.max);

    const uint16 = { min: 0, max: (2**16) - 1 };
    eq(test.PassAndReturnsUInt16(uint16.min), uint16.min);
    eq(test.PassAndReturnsUInt16(uint16.max), uint16.max);

    const int32 = { min: -(2**31), max: (2**31) - 1 };
    eq(test.PassAndReturnsInt32(int32.min), int32.min);
    eq(test.PassAndReturnsInt32(int32.max), int32.max);

    const uint32 = { min: 0, max: (2**32) - 1 };
    eq(test.PassAndReturnsUInt32(uint32.min), uint32.min);
    eq(test.PassAndReturnsUInt32(uint32.max), uint32.max);

    const int64 = { min: BigInt(2**63) * -1n, max: BigInt(2**63) - 1n };
    eq(test.PassAndReturnsInt64(int64.min), int64.min);
    eq(test.PassAndReturnsInt64(int64.max), int64.max);

    const uint64 = { min: BigInt(0), max: BigInt(2**64) - 1n };
    eq(test.PassAndReturnsUInt64(uint64.min), uint64.min);
    eq(test.PassAndReturnsUInt64(uint64.max), uint64.max);
}