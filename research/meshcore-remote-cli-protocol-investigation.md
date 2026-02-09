# MeshCore Remote CLI Command Protocol Investigation

**Date:** 2025-01-29  
**Issue:** Status 2 (InvalidParameter) when sending "neighbors" command to remote repeater  
**Test:** `Test_02_TryGetNeighborsAsync_ShouldSucceed`  
**Context:** Implementing neighbor list retrieval from remote MeshCore repeater/room server nodes

---

## Executive Summary

The .NET SDK is attempting to implement `TryGetNeighborsAsync()` to retrieve neighbor information from remote repeater nodes. The current implementation sends a "neighbors" text command via `CMD_SEND_TXT_MSG` with `txt_type=0x01`, but receives status code 2 (InvalidParameter) from the device.

**Key Question:** Is the payload format for sending remote CLI commands to repeaters/room servers correct, or is there a different protocol mechanism required?

---

## Problem Description

### Current Behavior

When sending the "neighbors" command to contact "SEAMESH_CORE_R1" (a known repeater), the device returns:
- **Status:** 2 (InvalidParameter)
- **Command:** CMD_SEND_TXT_MSG (0x02)
- **Contact:** A7F1661F95E4BB3DE6D2FBEB071208A58DBFC3418121445F8D119F9379A8B600 (BeepBoop repeater)

### Payload Sent

```
Hex: 3C310002A7F1661F95E4BB3DE6D2FBEB071208A58DBFC3418121445F8D119F9379A8B6000001BA9487696E65696768626F727300
```

**Breakdown:**
```
3C 31 00 02                 - Frame: start=0x3C, len=49, cmd=0x02 (CMD_SEND_TXT_MSG)
A7F1661F...79A8B600        - Target contact key (32 bytes)
00                          - Null separator
01                          - txt_type = 0x01 (command)
BA948769                    - Timestamp (4 bytes, little-endian = 1770087610)
6E65696768626F7273          - "neighbors" UTF-8
00                          - Null terminator
```

### Expected Behavior

The command should either:
1. Be accepted and return `RESP_CODE_SENT` followed by `RESP_CODE_CONTACT_MSG_RECV` with binary neighbor data
2. Or indicate the correct protocol mechanism for querying remote node information

---

## Research Questions

### 1. Protocol Mechanism Clarification

**Primary Question:** Is "neighbors" a text-based CLI command sent via `CMD_SEND_TXT_MSG`, or is there a different binary protocol command?

**Sub-questions:**
- Does the Python CLI use `CMD_SEND_TXT_MSG` for neighbor requests?
- Is there a dedicated binary command for neighbor queries?
- Do repeaters expose a text-based CLI interface via the companion protocol?

### 2. CMD_SEND_TXT_MSG Payload Format

**Current Implementation Analysis:**

The .NET SDK constructs:
```
[contact_key(32)][null][txt_type(1)][timestamp(4)][command_text][null]
```

**Questions:**
- Is the timestamp field required for commands (`txt_type=0x01`)?
- Should the timestamp come before or after `txt_type`?
- Is there a different payload structure for commands vs. regular messages?
- Are there any additional fields required (flags, sequence numbers, etc.)?

### 3. Authentication & Session Requirements

**Questions:**
- Must the client authenticate/login to the repeater before sending CLI commands?
- Is there a session establishment step required?
- Does the repeater need to be configured to accept CLI commands from specific contacts?
- Are there ACL (Access Control List) permissions that need to be set?

### 4. Python CLI Implementation

**Research Needed:**
- Locate `req_neighbours` or `fetch_all_neighbours` in the Python meshcore library
- Trace the actual bytes sent when requesting neighbors
- Identify if there's a login/authentication step before the command
- Document the complete sequence of operations

### 5. Firmware Protocol Handling

**Research Needed in MeshCore Firmware:**
- How does `BaseChatMesh.cpp` handle incoming `CMD_SEND_TXT_MSG`?
- What conditions cause status 2 (InvalidParameter)?
- Is `txt_type=0x01` recognized as a command type?
- How are CLI commands routed and processed?

### 6. Response Format Verification

**Current Expectation:**
```
Response: RESP_CODE_CONTACT_MSG_RECV (0x14)
Payload: [resp_code][sender_key_prefix(7)][txt_type][timestamp(4)][binary_data]
```

**Binary Neighbor Data Expected:**
```
[neighbours_count(u16)][results_count(u16)][neighbor_entries...]
Each entry: [pubkey(32)][secs_ago(u32)][snr(f32)] = 40 bytes
```

**Questions:**
- Is this the correct response format?
- Does the firmware send neighbor data as binary or text?
- What encoding is used for the response payload?

---

## Key Files for Investigation

### 1. Python CLI Reference Implementation

**File:** `C:\Users\wayneb.REDMOND\Documents\meshcore-cli\src\meshcore_cli\meshcore_cli.py`

**Focus Areas:**
- Line ~1500-2000: Look for `req_neighbours`, `fetch_all_neighbours`
- Search for functions that communicate with repeaters
- Identify the command structure and payload format
- Check for login/authentication sequences

**Code Patterns to Find:**
```python
def req_neighbours(...)
def fetch_all_neighbours(...)
# Look for references to "neighbors", "neighbour", "remote CLI"
```

### 2. MeshCore Python Library (meshcore module)

**Expected Location:** `meshcore-cli/src/meshcore/meshcore.py` or similar

**Focus:** The actual protocol implementation that `meshcore_cli.py` uses

### 3. Firmware Protocol Documentation

**File:** `C:\Users\wayneb.REDMOND\Documents\MeshCore\docs\companion_protocol.md`

**Research:**
- CMD_SEND_TXT_MSG specification
- txt_type field values and meanings
- Remote CLI command protocol section
- Authentication requirements

### 4. Firmware Payload Documentation

**File:** `C:\Users\wayneb.REDMOND\Documents\MeshCore\docs\payloads.md`

**Research:**
- Text message payload formats
- Binary response formats for neighbor data
- Command vs. message payload differences

### 5. Firmware Implementation

**Files:**
- `C:\Users\wayneb.REDMOND\Documents\MeshCore\src\helpers\BaseChatMesh.cpp`
- `C:\Users\wayneb.REDMOND\Documents\MeshCore\src\helpers\BaseChatMesh.h`
- `C:\Users\wayneb.REDMOND\Documents\MeshCore\src\Mesh.cpp`

**Focus Areas:**
- `handleTextMessage()` or similar function
- Command parsing for `txt_type=0x01`
- Neighbor list query handling
- Status code 2 error conditions

---

## Protocol Comparison Matrix

| Aspect | .NET SDK (Current) | Python CLI (To Research) | Firmware (To Research) |
|--------|-------------------|-------------------------|----------------------|
| Command Type | CMD_SEND_TXT_MSG (0x02) | ? | ? |
| txt_type Value | 0x01 | ? | ? |
| Timestamp Included | Yes (4 bytes) | ? | ? |
| Timestamp Position | After txt_type | ? | ? |
| Authentication | None | ? | ? |
| Command Text | "neighbors" | ? | ? |
| Null Terminators | Before txt_type, after text | ? | ? |
| Response Code | Expecting 0x14 | ? | ? |

---

## Test Case Details

### Test Environment
- **Device:** COM3 (Heltec V3 running v1.12.0-e738a74)
- **Target Contact:** BeepBoop (SEAMESH_CORE_R1)
- **Contact Type:** Repeater (type=2)
- **Contact Key:** A7F1661F95E4BB3DE6D2FBEB071208A58DBFC3418121445F8D119F9379A8B600

### Successful Contact Retrieval
The test successfully retrieved 350+ contacts including the target repeater, confirming:
- Basic device communication works
- Contact database is accessible
- The repeater contact exists and is valid

### Failed Neighbor Request
```
[Debug] Sending CMD_SEND_TXT_MSG with txt_type=0x01 (command) to BeepBoop, command: neighbors
[Debug] Sending command: 2 to device: COM3
[Information] Sending pkt 3C310002A7F1661F...6E65696768626F727300 (len=49)
[Debug] Received data: 3E02000102
[Error] Protocol error for command: 2, status: 2
MeshCore.Net.SDK.Exceptions.ProtocolException: Command 2 failed with status 2: Invalid contact or command parameters
```

---

## Hypotheses to Investigate

### Hypothesis 1: Missing Authentication
**Theory:** Repeaters require authentication/login before accepting CLI commands.

**Investigation Steps:**
1. Search Python CLI for login/password sequences
2. Check if there's a `login` command sent before `neighbors`
3. Review firmware for authentication requirements

**Expected Evidence:**
- Python CLI sends a login command with password
- Firmware checks authentication state before processing CLI commands

### Hypothesis 2: Wrong Payload Structure
**Theory:** The payload format for commands differs from regular messages.

**Investigation Steps:**
1. Compare .NET SDK payload with Python CLI actual bytes
2. Check if timestamp position or format is different
3. Verify null separator positions

**Expected Evidence:**
- Timestamp might not be required for commands
- Different field ordering or additional fields

### Hypothesis 3: Different Protocol Mechanism
**Theory:** Neighbor queries use a dedicated binary command, not text CLI.

**Investigation Steps:**
1. Look for binary protocol commands in firmware (CMD_REQ_NEIGHBORS or similar)
2. Check if Python CLI uses a different command code
3. Review companion_protocol.md for neighbor query commands

**Expected Evidence:**
- A dedicated command code for neighbor queries
- Binary payload format instead of text

### Hypothesis 4: txt_type Value Incorrect
**Theory:** The value 0x01 for txt_type is not correct for commands.

**Investigation Steps:**
1. Search firmware for txt_type usage and valid values
2. Check Python CLI for txt_type values
3. Review protocol documentation

**Expected Evidence:**
- Different txt_type value for CLI commands
- Documentation of txt_type field meanings

### Hypothesis 5: Contact Key Format
**Theory:** The contact key needs to be formatted differently.

**Investigation Steps:**
1. Verify the contact key is the full 32-byte public key
2. Check if a key prefix or hash is used instead
3. Compare with Python CLI

**Expected Evidence:**
- Different key representation (prefix, hash, etc.)

---

## Required Deliverables

### 1. Protocol Specification Document
**Must Include:**
- Exact payload format for remote CLI commands
- Field-by-field breakdown with data types and byte order
- Authentication requirements and sequence
- Response format specification

### 2. Python CLI Code Analysis
**Must Include:**
- Complete code path for neighbor request
- Actual bytes sent (hex dump if possible)
- Any setup/authentication steps
- Response parsing logic

### 3. Firmware Behavior Documentation
**Must Include:**
- How CMD_SEND_TXT_MSG with txt_type=0x01 is processed
- Status 2 error conditions
- Required payload structure
- Authentication/permission checks

### 4. Working Example
**Must Include:**
- Byte-by-byte payload that successfully requests neighbors
- Expected response bytes
- Complete sequence of commands if multiple steps required

### 5. Root Cause Analysis
**Must Include:**
- Specific reason for status 2 error
- What's different between .NET implementation and working Python CLI
- Required changes to fix the issue

### 6. Implementation Guidance
**Must Include:**
- Updated payload construction code
- Any prerequisite commands needed
- Response parsing logic
- Error handling recommendations

---

## Success Criteria

The research is complete when we can answer:

1. ? **How does the Python CLI successfully request neighbors from a repeater?**
   - Exact command sequence
   - Exact payload bytes
   - Authentication steps (if any)

2. ? **Why is the .NET SDK getting status 2 (InvalidParameter)?**
   - Specific field or value that's incorrect
   - Missing required step
   - Wrong protocol mechanism

3. ? **What changes are needed to fix the .NET SDK implementation?**
   - Code changes with exact field values
   - Sequence of operations
   - Response handling

4. ? **Is the expected response format correct?**
   - Binary structure validation
   - Field sizes and byte order confirmation

---

## Investigation Priority

### High Priority (Must Have)
1. Python CLI `req_neighbours`/`fetch_all_neighbours` implementation
2. Actual payload bytes sent by Python CLI
3. Firmware CMD_SEND_TXT_MSG handling for txt_type=0x01
4. Status 2 error conditions in firmware

### Medium Priority (Should Have)
1. Authentication/login requirements
2. Alternative binary protocol commands
3. Complete protocol documentation review
4. Response format validation

### Low Priority (Nice to Have)
1. Historical context of protocol changes
2. Alternative approaches (if any)
3. Performance considerations
4. Edge cases and error scenarios

---

## Investigation Approach

### Phase 1: Python CLI Deep Dive (Day 1)
1. Locate neighbor request implementation
2. Trace actual bytes sent
3. Identify authentication steps
4. Document complete sequence

### Phase 2: Firmware Analysis (Day 1-2)
1. Review CMD_SEND_TXT_MSG handling
2. Understand txt_type processing
3. Identify status 2 conditions
4. Document expected payload format

### Phase 3: Protocol Documentation (Day 2)
1. Review companion_protocol.md
2. Review payloads.md
3. Cross-reference with code findings
4. Identify any gaps or clarifications needed

### Phase 4: Synthesis & Solution (Day 2-3)
1. Compare all findings
2. Identify root cause
3. Design solution
4. Create implementation plan

---

## Contact Points

**Primary Researcher:** MeshCore Protocol Expert  
**Stakeholder:** .NET SDK Development Team  
**Subject Matter Experts:**
- MeshCore firmware developers
- Python CLI maintainers
- Protocol documentation authors

---

## Appendix A: Related Code Snippets

### .NET SDK Current Implementation

```csharp
// From MeshCoreClient.cs - SendRemoteCommandAsync()
var payload = new List<byte>();
payload.AddRange(contactKeyBytes);  // 32-byte contact public key
payload.Add(0x00);                  // null separator
payload.Add(0x01);                  // txt_type = 1 (command)

// Add timestamp (4 bytes, little-endian)
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var timestampBytes = BitConverter.GetBytes((uint)timestamp);
if (!BitConverter.IsLittleEndian)
{
    Array.Reverse(timestampBytes);
}
payload.AddRange(timestampBytes);

payload.AddRange(commandBytes);     // command text
payload.Add(0x00);                  // null terminator
```

### Expected Python CLI Pattern (To Research)

```python
# Expected pattern to find in meshcore_cli.py
def req_neighbours(mc, contact):
    # Look for command construction
    # Look for mc.send_command() or similar
    # Document the payload
    pass

def fetch_all_neighbours(mc, contact):
    # Look for response parsing
    # Document binary structure
    pass
```

---

## Appendix B: Hex Dump Analysis

### Sent Payload Detailed Breakdown

```
Offset  Hex                                              ASCII
------  -----------------------------------------------  -----------------
0000    3C 31 00 02                                      <1..
        ??? Frame header: start, length, command

0004    A7 F1 66 1F 95 E4 BB 3D E6 D2 FB EB 07 12 08 A5  ..f....=........
0014    8D BF C3 41 81 21 44 5F 8D 11 9F 93 79 A8 B6 00  ...A.!D_....y...
        ??? 32-byte contact public key

0024    00                                               .
        ??? Null separator

0025    01                                               .
        ??? txt_type = 0x01 (command)

0026    BA 94 87 69                                      ...i
        ??? Timestamp: 0x698794BA = 1770087610 (2025-01-29 ~18:06 UTC)

002A    6E 65 69 67 68 62 6F 72 73                       neighbors
        ??? Command text: "neighbors"

0033    00                                               .
        ??? Null terminator
```

### Received Response

```
Offset  Hex              ASCII
------  ---------------  -----
0000    3E 02 00 01 02   >....
        ?  ?  ?  ?  ??? Status: 0x02 (InvalidParameter)
        ?  ?  ?  ?????? Response code: 0x01 (RESP_CODE_ERR)
        ?  ?  ????????? Command echo: 0x00 (or padding)
        ?  ???????????? Length: 2
        ??????????????? Start byte: 0x3E (inbound frame)
```

---

## Appendix C: MeshCore Status Codes

```csharp
// From .NET SDK MeshCoreStatus enum
public enum MeshCoreStatus : byte
{
    Success = 0x00,
    InvalidCommand = 0x01,
    InvalidParameter = 0x02,    // ? Our error
    DeviceError = 0x03,
    NetworkError = 0x04,
    TimeoutError = 0x05,
    // ... others
}
```

**Status 2 (InvalidParameter) typically indicates:**
- Malformed payload structure
- Invalid field values
- Missing required fields
- Wrong parameter types or sizes

---

## Appendix D: Questions for Firmware Developers

If direct access to firmware developers is possible:

1. What is the correct payload structure for CMD_SEND_TXT_MSG with txt_type=0x01?
2. Is authentication required before sending CLI commands to repeaters?
3. What specifically causes status 2 (InvalidParameter) for this command?
4. Is there a dedicated binary command for querying neighbors?
5. Should the timestamp be included in command payloads?
6. Are there any undocumented protocol changes affecting this functionality?

---

## Appendix E: Alternative Approaches to Explore

If the current approach proves fundamentally flawed:

1. **Binary Protocol Command**: Look for CMD_REQ_NEIGHBORS or similar
2. **Different Message Type**: Perhaps not CMD_SEND_TXT_MSG at all
3. **WebSocket/HTTP API**: Some nodes may expose REST APIs
4. **Direct Serial CLI**: Bypass companion protocol, use serial CLI directly
5. **Mesh Network Query**: Query via mesh routing rather than direct command

---

**End of Research Prompt**

**Next Steps After Research:**
1. Update .NET SDK implementation based on findings
2. Add comprehensive unit tests
3. Update protocol documentation
4. Create example code for users
5. Add troubleshooting guide

