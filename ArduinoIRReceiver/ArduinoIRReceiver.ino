#include <IRremote.hpp>

constexpr uint8_t IR_RECEIVE_PIN = 2;

void setup()
{
    Serial.begin(115200);
    IrReceiver.begin(IR_RECEIVE_PIN, DISABLE_LED_FEEDBACK);
}

void loop()
{
    if (IrReceiver.decode())
    {
        if (IrReceiver.decodedIRData.protocol != UNKNOWN)
        {
            char code[9];
            snprintf(code, sizeof(code), "%08lX",
                     (unsigned long)IrReceiver.decodedIRData.decodedRawData);
            Serial.println(code);
        }

        IrReceiver.resume();
    }
}
