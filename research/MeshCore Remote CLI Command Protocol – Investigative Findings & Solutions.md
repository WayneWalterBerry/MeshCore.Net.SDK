# MeshCore Remote CLI Command Protocol – Investigative Findings & Solutions


## 1. **Understanding Remote CLI vs. Binary Protocol**

**Neighbors via CLI Command:** The MeshCore **“neighbors”** query is designed as a **command-line interface (CLI) command** available on repeaters (not a separate binary message type). It can be executed remotely by sending it as a *text command* to the repeater via the companion protocol. In other words, you do **not** need a special binary command for retrieving the neighbor list – you send the text `"neighbors"` as a remote CLI command. The key is to format that command **exactly** as the MeshCore companion protocol expects. [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference), [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)

**Status Request Alternative:** MeshCore firmware also provides a general **binary status query** (`CMD_SEND_STATUS_REQ`, command code `27`) that can retrieve certain node information (like neighbor data) in a structured binary form. However, the intended approach for an on-demand neighbor list from a repeater is typically to use the CLI command mechanism (with the proper format), mirroring what the Python CLI does. The `CMD_SEND_STATUS_REQ` could be an alternative if the firmware is specifically designed to include neighbor info in its status response, but using the CLI “neighbors” command is the more direct method and is confirmed by the CLI documentation as the way to get the neighbor table remotely. [\[github.com\]](https://github.com/meshcore-dev/MeshCore/wiki/Companion-Radio-Protocol), [\[github.com\]](https://github.com/meshcore-dev/MeshCore/wiki/Companion-Radio-Protocol) [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference), [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)

## 2. **Correct Payload Format for `CMD_SEND_TXT_MSG` (Remote CLI)**

Your current implementation was rejected with status code 2 (`InvalidParameter`) because the payload layout did not match the expected format. The **MeshCore companion protocol defines a strict structure for `CMD_SEND_TXT_MSG` frames** (command code 0x02) which must be followed. The correct payload format for sending a remote CLI text command is:

*   **Command Code (1 byte):** `0x02` for `CMD_SEND_TXT_MSG` (this is placed as the first byte of the payload, immediately after the frame length in the serial frame).
*   **`txt_type` (1 byte):** Type of text message. For a remote CLI command, use `0x01` (the firmware defines `TXT_TYPE_COMMAND = 1`). *(By convention, `0x00` is used for normal text/chat messages.)*
*   **Attempt Number (1 byte):** A sequence number (0–3) used for retries. For a new command, use `0x00` on the first attempt.
*   **Sender Timestamp (4 bytes, little-endian `uint32`):** A timestamp (e.g. current epoch time or seconds since device boot) to tag the command. The firmware uses this to order or identify the command. (Earlier documentation references a “two-byte timestamp” for remote CLI, but the current protocol uses a 32-bit timestamp field, which matches your use of 4 bytes `BA 94 87 69`.) [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)
*   **Target Contact Key Prefix (6 bytes):** The **first 6 bytes of the target node’s public key**. This identifies the destination repeater. **Do not send the full 32-byte key** here – the protocol expects only a 6-byte prefix. The MeshCore firmware uses 6-byte public key prefixes as unique contact identifiers to reduce payload size in messaging commands. (For example, other commands like `CMD_REMOVE_CONTACT` also use a 6-byte key prefix to specify a contact.) [\[deepwiki.com\]](https://deepwiki.com/meshcore-dev/MeshCore/4.1.1-command-protocol)
*   **Command Text (variable length):** The ASCII bytes of the CLI command string (`"neighbors"` in this case). This field extends to the end of the frame. **Terminate the string with a null byte (`0x00`)** to mark the end of the command text. (The null terminator is recommended to ensure the firmware’s command parser recognizes the end of the command string.)

**Diagram – Correct `CMD_SEND_TXT_MSG` Payload Structure:**


**Important Corrections for Your Implementation:**

*   **Order of Fields:** In your attempt, you placed the 32-byte contact key at the beginning of the payload, followed by a null byte and then the `txt_type`. This is wrong. As shown above, the `txt_type` must immediately follow the command code in the payload. By starting the payload with the contact key, the repeater interpreted the first byte of the key (`0xA7` in your example) as the `txt_type`, which is not a valid type (valid types are small integers like 0 or 1). This led to an immediate rejection with `InvalidParameter`. The correct position for the destination identifier is **after** the timestamp field, and it should be a 6-byte prefix, not the full key.

*   **Contact Identifier Length:** Use the **6-byte public key prefix** for the contact. Your payload included the entire 32-byte public key of *SEAMESH\_CORE\_R1*. The protocol expects only the first 6 bytes of the public key in messaging commands for contacts. Sending a 32-byte key in this field overwhelms the parser (and combined with the mis-ordering, completely corrupts the frame structure). The firmware likely didn’t find a matching contact for that prefix (since it tried to interpret part of your timestamp or text as the prefix) and thus flagged the parameters as invalid.

*   **Omitted Fields:** Include the `attempt` byte. The MeshCore `CMD_SEND_TXT_MSG` frame includes an “attempt” counter byte (for retry tracking) immediately after the txt\_type. Your implementation did not provide this byte (you inserted a 0x00 as a separator after the 32-byte key, which was misinterpreted). In the corrected format, you should explicitly send an `attempt` value. Typically this will be 0x00 for the first try; if the command were retried due to no response, it might be 0x01, and so on (up to 3). Ensure this byte is present in the payload.

*   **No Separator Needed:** Do not insert extra separator bytes except the one null at the end of the command string. In your payload, you added a `0x00` after the 32-byte key as a separator. The MeshCore protocol doesn’t use a null byte between structured fields – it relies on fixed field lengths. The only null terminator should be at the *end* of the text string. By adding an unexpected `0x00` early, you effectively shifted the positions of all subsequent fields. The firmware likely parsed that 0x00 as the `txt_type` (which would be interpreted as “plain text” type), and then the next byte (0x01) as the `attempt`, etc., ultimately causing the frame to be nonsense. Remove this extraneous separator.

**Fixed Payload Example:** If we apply these corrections to your case, the payload (not including the USB framing bytes `0x3C/0x3E` and length) would be:

    02 01 00 [timestamp_4_bytes] [recipient_key_prefix_6_bytes] 6E 65 69 67 68 62 6F 72 73 00

Breaking it down:

*   `02`: `CMD_SEND_TXT_MSG` code.
*   `01`: `txt_type = 0x01` (indicates a CLI command text).
*   `00`: Attempt number (0 for first attempt).
*   `[timestamp_4_bytes]`: e.g. `BA 94 87 69` for the 32-bit Unix timestamp 1770087610 (0x698794BA) you used.
*   `[recipient_key_prefix_6_bytes]`: the first 6 bytes of the repeater’s public key (for *A7F1661F95E4BB3D...*, the prefix bytes would be `A7 F1 66 1F 95 E4`). [\[pe1hvh.nl\]](https://www.pe1hvh.nl/pdf/MeshCore_Packet_Structure_EN.pdf)
*   `6E 65 69 67 68 62 6F 72 73`: ASCII for `"neighbors"`. [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)
*   `00`: null terminator for the string.

*(Note: The USB/serial frame would wrap the above payload with the delimiter and length: e.g., for a 23-byte payload, you’d see: `< 0x17 0x00` …payload… in the raw bytes. The outbound frame uses `0x3C` (“<”) as start, little-endian length, then the payload.)* [\[github.com\]](https://github.com/meshcore-dev/MeshCore/wiki/Companion-Radio-Protocol)

With this corrected format, the repeater will recognize the command properly. It should return a **response** indicating the command execution results (more on this below).

## 3. **Authentication and Access Control**

**Check for Login Requirements:** MeshCore repeaters and room servers implement an Access Control List (ACL) and optional password protection for remote management. By default, **admin commands require authentication**. Each repeater has an admin password (default is `"admin"`) and an optional guest (read-only) password (default `"hello"`). If the device has *not* been configured to allow anonymous read-only access (`allow.read.only` setting), attempting to run any CLI commands without logging in could be rejected by the device. Thus, before sending the `neighbors` command, your application should perform a login sequence unless you are certain the repeater allows unauthenticated read access. [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference), [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)

**How to Log In:** Use the `CMD_SEND_LOGIN` command (command code 26) to authenticate with the remote node. The payload format for `CMD_SEND_LOGIN` is:

*   `0x1A` (26) for the command code,
*   followed by the 32-byte public key of the target node (the repeater’s full ID),
*   and then the password string (UTF-8), terminated by `0x00`.

For example, to log in to *SEAMESH\_CORE\_R1* with the default admin password, you would send a `CMD_SEND_LOGIN` frame containing the repeater’s 32-byte key and the ASCII string `"admin"` followed by a null byte. On success, the repeater will reply with `PUSH_CODE_LOGIN_SUCCESS (0x85)` and grant your session appropriate permissions. After a successful login (Admin mode), you’ll be authorized to run privileged CLI commands like `neighbors` and others (if the login fails or is omitted when required, the device may reject even well-formed commands).

**ACL Considerations:** Ensure that your device (the companion radio or client node sending the command) is known to the repeater’s contact list/ACL. In MeshCore, repeaters can restrict which nodes are allowed to issue remote commands. Given that you were able to retrieve the repeater’s contact info and the command was received (resulting in a response), we can infer the repeater had your device in its contacts (as *BeepBoop* with the given key) and did attempt to parse your command. So *contact discovery* is not the issue here – but authenticating for CLI access might still be required if the repeater’s policy demands it (depending on `allow.read.only` setting). [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)

## 4. **Firmware Behavior and Error Analysis**

The MeshCore firmware (see `BaseChatMesh.cpp`) strictly validates incoming frames for correct formatting. A malformatted frame will trigger an error response. In your test, the repeater responded with:

    3E 02 00 01 02

This corresponds to a **response frame** (`0x3E` start byte for outbound frames) with a **2-byte payload**. Decoding: [\[github.com\]](https://github.com/meshcore-dev/MeshCore/wiki/Companion-Radio-Protocol)

*   `0x01` is `RESP_CODE_ERR` (a generic “command failed” response code). [\[deepwiki.com\]](https://deepwiki.com/meshcore-dev/MeshCore/4.1.1-command-protocol)
*   The following byte `0x02` is the specific error **status code** (value 2 = “Invalid Parameter”). [\[deepwiki.com\]](https://deepwiki.com/meshcore-dev/MeshCore/4.1.1-command-protocol)

The firmware issued an `InvalidParameter` error because it detected that the fields of your `CMD_SEND_TXT_MSG` were not as expected. Likely reasons the firmware flagged it invalid include:

*   **Unexpected txt\_type value:** As explained, the first byte after the command code was misinterpreted (0xA7 from the key) as the `txt_type`, which is out of the valid range. The firmware’s parser found an unknown `txt_type` and aborted the command.
*   **Message length mismatch:** The extra bytes (sending 32+ bytes for the key and adding an extra null) made the payload longer than necessary. The firmware may have parsed garbage data into the fields (e.g., treating part of the key as the attempt or timestamp), causing it to reject the frame for having an invalid format or inconsistent length. The MeshCore protocol implementation expects the frame to **exactly** match the defined structure; any deviation can result in a parameter error.

Inside `BaseChatMesh.cpp`, the `handleTextMessage()` (which processes `CMD_SEND_TXT_MSG`) likely performs checks such as:

*   Is the `txt_type` one of the allowed values? If not, return error.
*   Does the payload length match the minimum required for the fields? (At least 1+1+1+4+6 bytes before text). If not, return error.
*   Can it find the target contact by the provided prefix? If not, return an error.

Your payload failed these checks. The **first mismatched byte cascaded into a structurally invalid message**, hence the broad “invalid parameter” response. The firmware never even got to attempt executing the “neighbors” command because the frame was considered malformed.

## 5. **Expected Device Response and Output Handling**

Once you correct the payload and send the `neighbors` command properly (and handle login if needed), the repeater will execute the command and return its neighbor list. Here’s what to expect:

*   **Response Code:** The initial response will be `RESP_CODE_SENT (0x00)` indicating the command was dispatched to the contact. (If using protocol version 3+, you might get a `RESP_CODE_OK` instead after the whole sequence, but traditionally `RESP_CODE_SENT` acknowledges the message was radio-sent.) [\[deepwiki.com\]](https://deepwiki.com/meshcore-dev/MeshCore/4.1.1-command-protocol)

*   **Neighbor Data Delivery:** The neighbor list data from the repeater will arrive **as one or more “contact message received” frames**, since the repeater sends the output as if it were a message. In MeshCore, remote CLI output is often delivered as a special incoming message from the target node. Each such frame uses `RESP_CODE_CONTACT_MSG_RECV` (historically code 0x07, or 0x10/0x17 in newer protocol versions) and contains the output text or data. For the `neighbors` command, the firmware may send the data **in a structured binary format** for efficiency (instead of plain text lines) when responding over the air. In that case, the payload of the received message would include the neighbor information in a binary form (the Python CLI likely parses this and then formats it into human-readable lines for display).

    *   Based on MeshCore development discussions, the **binary format for `neighbors` output** is expected to start with two 16-bit numbers: the total neighbor count and the count of *“recent”* neighbors (for example, some firmwares distinguish *currently active* vs *historical* neighbors). These are then followed by an array of neighbor entries.
    *   **Neighbor Entry Structure:** Each entry is 40 bytes: a 32-byte neighbor **Public Key** (Node ID), a 4-byte **“seconds ago”** timestamp indicating last heard time, and a 4-byte **SNR value** (as a float or fixed-point integer, SNR\*4) for the last received advertisement. [\[github.com\]](https://github.com/meshcore-dev/MeshCore/wiki/Repeater-&-Room-Server-CLI-Reference), [\[github.com\]](https://github.com/meshcore-dev/MeshCore/wiki/Repeater-&-Room-Server-CLI-Reference)

    For instance, after sending the `neighbors` command, the repeater might reply with a frame whose payload (after the initial response code) looks like:

        [NeighborCount(u16)] [RecentCount(u16)] 
        [Neighbor1 PubKey(32 bytes) | LastHeard_sec(u32) | SNR_x4(u32)] 
        [Neighbor2 PubKey(32) | LastHeard_sec(u32) | SNR_x4(u32)]
        ... (and so on for each neighbor)

    Your code should be prepared to receive one or multiple `RESP_CODE_CONTACT_MSG_RECV` frames containing this data. In some implementations, the entire neighbor table might fit in one frame; if not, you could get multiple message frames (each with a portion of the output). Each such frame may also include the text type and timestamp as per the message format (e.g., for protocol v3: 6-byte sender prefix, 1-byte path length, 1-byte txt\_type, 4-byte timestamp, then data). The two-byte “prepended timestamp” mentioned in older docs refers to the timestamp included in remote CLI text lines; in current designs this is encompassed by the 4-byte `sender_timestamp` field in the message payload. [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)

*   **Parsing the Output:** If the neighbor data is binary as described, your `TryGetNeighborsAsync()` method should parse the incoming message payload accordingly (read the counts and loop through each 40-byte entry to extract the neighbor’s public key, last-heard time, and signal info). If instead the firmware returns plain text lines (less likely in recent firmware), you would receive one or more text messages containing lines like `"AB12CD34: 120 sec ago, SNR=10.5"` – but again, given the design of the companion protocol, a binary format is anticipated for such data-heavy output.

## 6. **Summary & Next Steps**

**Why the .NET implementation failed:** The `InvalidParameter` error was caused by a malformatted `CMD_SEND_TXT_MSG` frame. The contact key was mis-placed and too long, and the required fields (`txt_type`, `attempt`, etc.) were not in the expected order. The repeater could not interpret the command and rejected it without execution.

**How to fix the issue:** Construct the `CMD_SEND_TXT_MSG` payload exactly as per the MeshCore spec:

1.  **Reorder fields** – The byte immediately after the `0x02` command code must be the `txt_type` (0x01 for commands), not the contact key. Follow it with the attempt byte and timestamp, then the 6-byte key prefix, and finally the ASCII command string.
2.  **Use 6-byte key prefix** – Provide only the first 6 bytes of the target’s public key (e.g. `A7F1661F95E4` for the example repeater) in the payload instead of the full 32-byte key.
3.  **Include all required bytes** – Don’t omit the `attempt` field, and remove any superfluous separator bytes. Only the command string should be null-terminated.
4.  **Authenticate if needed** – If the repeater requires a login, send a `CMD_SEND_LOGIN (26)` with the repeater’s 32-byte key and the password (e.g. `"admin"` by default) before issuing the neighbors command. A successful login will grant the necessary rights for remote commands.

By implementing these changes, the `TryGetNeighborsAsync()` call should succeed. The repeater will process the “neighbors” command and return the neighbor list data. In practice, the official Python CLI tool follows this same procedure:

*   It likely sends a login frame if the node is password-protected (the MeshCore CLI can prompt for a password or use a stored one), and then
*   sends the `neighbors` command with a properly formatted `CMD_SEND_TXT_MSG` frame.

With the corrected protocol usage, your .NET SDK should mirror that behavior. The repeater’s response can then be parsed to obtain the list of neighbor nodes and their link stats.

## 7. **Confirmation & Final Checks**

To ensure everything is working:

*   Monitor that the first response to your command is a quick **acknowledgment** (e.g., `RESP_CODE_SENT` or `RESP_CODE_OK` from the companion radio), meaning the command frame was forwarded to the target. [\[deepwiki.com\]](https://deepwiki.com/meshcore-dev/MeshCore/4.1.1-command-protocol)
*   Then capture the incoming **message frames** from the repeater. If you see frames with `RESP_CODE_CONTACT_MSG_RECV` containing data (and the prefix matching the repeater’s ID), that means the repeater’s output is arriving. Decode these according to the expected format (as described above for neighbor data). If instead you get a `RESP_CODE_ERR` with a non-zero error code again, re-check the payload formatting and ensure the node’s ACL/password requirements are satisfied.

By adhering to the official MeshCore companion protocol specification, you will be able to retrieve the neighbor list successfully. In summary, **the .NET SDK should be updated to use the correct field order and sizes for `CMD_SEND_TXT_MSG`, and perform a login first if the repeater is not open for remote commands.** These changes will resolve the Status 2 error and allow the `TryGetNeighborsAsync()` test to pass, returning the expected neighbor information from the remote repeater.


**References:**

*   MeshCore Companion Protocol Documentation – *“Command Protocol”* (meshcore-dev/MeshCore Wiki) – defines the frame structure for `CMD_SEND_TXT_MSG` and other commands.
*   MeshCore CLI Reference (CommonCLI) – explains that `neighbors` is a repeater CLI command accessible remotely, and notes the behavior for remote CLI commands (timestamp prepended, same parsing rules). [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference), [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)
*   MeshCore Firmware source (`BaseChatMesh.cpp` & related headers) – implements parsing of incoming frames and will return `RESP_CODE_ERR 0x02 (Invalid Parameter)` if any fields are out of place or invalid.
*   MeshCore Wiki – *Repeater & Room Server CLI Reference* – shows example CLI usage of `neighbors` and other commands, and mentions the ACL and password system for remote management of repeaters. [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference), [\[wiki.meshcoreaus.org\]](https://wiki.meshcoreaus.org/books/doc-firmware/page/doc-cli-reference)
