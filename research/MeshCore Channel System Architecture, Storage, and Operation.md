\# MeshCore Channel System: Architecture, Storage, and Operation





&nbsp;\[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1), \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



\## 1. What Are MeshCore Channels?



\*\*MeshCore channels\*\* (or \*group channels\*) are a core feature that enable one-to-many encrypted communication across the mesh network. In MeshCore, a channel represents a shared conversation space that multiple nodes can join. All messages sent on a channel are encrypted with a common \*\*pre-shared key (PSK)\*\*, and any node that possesses this key can decrypt and participate. This is analogous to a group chat or radio net: it’s a logical grouping of nodes for broadcast messaging.



Each MeshCore node (device) can be configured with a set of channels that it knows about. Channels exist \*\*above\*\* the basic routing layer – they are an application-layer concept in the \*\*BaseChatMesh\*\* framework (which implements texting over the mesh). At the routing level, all packets still traverse the mesh via repeaters and learned paths, but the channel determines \*who\* should process a given message. In effect, channels act as an \*\*addressing mechanism\*\*: instead of messages addressed to a single recipient, a channel message is addressed to all nodes in that channel (using a short channel identifier in the packet header rather than a specific destination). Nodes not in the channel will ignore the message (or be unable to decrypt it), while members will accept and decrypt it. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/4.6-t-deck-ultra-firmware), \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/4.6-t-deck-ultra-firmware) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



\*\*Relation to Contacts:\*\* Channels complement MeshCore’s direct peer-to-peer contacts. A \*contact\* in MeshCore is an individual node identity you’ve discovered (via exchange of Ed25519 public keys in adverts) and with whom you’ve established a unique shared secret for direct messaging. Contacts enable \*\*one-to-one encrypted chats\*\* using per-peer ECDH keys. A channel, on the other hand, enables \*\*many-to-many group chats\*\* using a shared symmetric key known to the group. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



Importantly, channels do \*\*not\*\* replace contacts – they are layered on top of the MeshCore network. You still have your Ed25519 identity and contact list for routing and acknowledgments, but a channel provides a way to send a single message that \*any number of contacts\* can receive. In fact, \*\*channel membership is not explicitly tied to contact entries\*\* – joining a channel doesn’t automatically create contacts, nor is there an internal list of members stored with the channel. Instead, \*knowing the channel’s secret\* effectively makes you part of that multicast group. Any node with the key will be able to decrypt channel messages and thus implicitly “participate.” There’s no built-in roster of participants; presence is determined dynamically by who sends or receives messages on that frequency and key.



\*\*Channels vs. Broadcasts:\*\* In MeshCore, some network packets (like discovery “adverts”) are broadcast to all nearby nodes without encryption. Channels are different from these raw broadcasts – channels \*\*encrypt\*\* the content and restrict it to intended recipients. A message on a channel is typically transmitted as a \*\*flood\*\* (so it will propagate through repeaters network-wide), but only nodes that share the channel key can actually read it. This adds confidentiality and group segmentation that a pure broadcast lacks. In summary, channels allow you to create \*\*virtual sub-networks\*\* within the larger mesh: each channel is like a separate chat room with its own encryption key. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



\## 2. Channel Types: Public vs. Private Channels



MeshCore supports different types of channels, broadly falling into two categories: \*\*public (open) channels\*\* and \*\*private channels\*\*. All channels function similarly at a technical level (each has a name and a secret key), but how their keys are chosen and shared differs, which in turn affects how the channel is used and who can join.




&nbsp;\[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology), \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1), \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



\*\*The “All” Channel (Public):\*\* Upon first setup, every MeshCore node includes a default channel often referred to as the \*\*Public\*\* or “All” channel. In the firmware, it’s typically configured as channel index 0 with the name “Public”. All devices share the \*same\* symmetric key for this channel, which is pre-set by the MeshCore developers. For example, the default public channel key is documented as `8b3387e9c5cdea6ac9e5edbaa115cd72` (hex) – this corresponds to the Base64 string `izOH6cXN6mrJ5e26oRXNcg==`. Because every device knows this key from the start, the Public channel acts as a \*\*common meeting place\*\*: nodes use it to advertise themselves and send initial messages network-wide. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



\*Security of Public Channel:\* The public channel is encrypted with the above well-known PSK, which prevents casual eavesdropping (i.e. random non-MeshCore radios won’t see plaintext). However, \*\*it is not secure against knowledgeable parties\*\*, since the key is public knowledge. Anyone with MeshCore or knowledge of the default key could listen in. Thus, you should treat the Public channel as an open forum – good for general chatter and discovery, but not for sensitive data. MeshCore documentation describes it as a global broadcast medium with “basic encryption to prevent casual eavesdropping”. In practice, mesh communities might use the Public channel for announcements or to find nearby nodes, then move to private channels for confidential discussions. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



\*\*Private Channels:\*\* Users can create their own channels with custom names and secrets. These are what we call private channels. Unlike the Public channel, the secret key for a private channel is generated by the user (often randomly) and \*\*shared only with intended members\*\* (typically out-of-band). Because the key isn’t known to others, the channel effectively becomes an \*\*invite-only space\*\*. Devices not given the secret cannot join or even understand the traffic on that channel. In fact, MeshCore nodes on different channel keys will \*ignore each other entirely\* – they won’t form contacts through adverts or forward each other’s messages. This enables users to set up \*\*closed meshes\*\* for privacy or organizational purposes. For example, a group of rescue workers might agree on a private channel key for their operations so that their MeshCore radios form an isolated mesh network that outsiders can’t monitor. \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



There are no hard-coded channel categories beyond this; any channel other than the default one is as “private” as you make it by controlling the key distribution. MeshCore does not currently define special channel types like “emergency” or “administrative” channels with different behaviors – those would just be private channels by convention (e.g. a channel named “Emergency” with a secret that a certain group knows could serve that role, but technically it’s not treated differently by the system). All channels use the same underlying mechanisms regardless of name or purpose.



\*\*Discovery and Routing Differences:\*\* The choice of channel affects how nodes discover each other and how messages propagate:



\*   On the Public channel, \*\*node discovery is automatic\*\*. All nodes periodically broadcast \*\*advertisements\*\* (identity beacons) on the public channel by default. This means any two MeshCore radios in range will see each other’s adverts and can add each other as contacts (even before exchanging any direct messages) as long as both have the public channel active. Also, repeaters will forward these public adverts across multiple hops, allowing distant nodes to become aware of each other’s presence on the mesh. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



\*   On a \*\*private channel\*\*, discovery is \*\*intentional\*\*. If a node switches to using only a private channel (disabling public adverts), it will only be “visible” to others who are on that same channel. MeshCore supports this as a privacy feature: devices can be configured to \*only\* advertise on a private channel, effectively hiding them from the wider mesh. The advert packets can even be encrypted with the channel’s secret, meaning outsiders who overhear them can’t extract the node’s identity or name. (This is analogous to how Meshtastic segregates networks by channel key – devices on a different channel key simply ignore or can’t decrypt each other’s transmissions.) \*\*Thus, private channels create isolated mesh segments:\*\* nodes exchange discovery beacons only within the group. To join such a channel, you typically have to be invited (e.g. someone gives you the channel QR code or link). Once you add the channel on your device, you’ll start hearing the adverts and messages on it and become part of that sub-network. \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



\*   \*\*Routing and communication\*\* work the same way for public vs private channels in terms of packet propagation. In both cases, messages are usually sent in flood mode (so that all repeaters retransmit them once) unless a more efficient path is learned. The key difference is just who can decrypt and respond. One nuance: on the public channel, since everyone is a participant, the network might be busier (more nodes chatting or advertising). On a private channel, the traffic is confined to that group, which can reduce congestion and also increase privacy.



In summary, \*\*public channels\*\* are open and default, great for getting onto the mesh initially, whereas \*\*private channels\*\* are custom-defined, require sharing a secret, and enable true confidentiality and controlled participation. MeshCore encourages using private channels for any communication that needs to be kept within a certain team or user group, and leaving the public channel for general reachability or inter-network bridging.



\## 3. Channel Data Structure and Storage (\*/channels2\* file)



Internally, MeshCore represents each channel with a simple fixed-size data structure called `ChannelDetails`. This structure holds all the information needed for a channel: essentially a \*\*name\*\* and the \*\*secret key\*\*, plus a little space for housekeeping. The channel list is stored persistently in a dedicated binary file on the device’s filesystem.



\*\*File and Format:\*\* Channels are saved in a file named \*\*`/channels2`\*\* in the device’s flash (or SD card, depending on platform). Each entry in this file corresponds to one channel configuration. The file is a flat array of fixed-length records for fast lookup by index. According to the MeshCore documentation, each `ChannelDetails` record is \*\*68 bytes\*\* long. The channel file can accommodate up to \*\*40 channels\*\* on companion devices (in practice, typically far fewer are used). \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



Let’s break down the `ChannelDetails` record layout as stored in `/channels2`:



\*   \*\*Name\*\* – 32 bytes: This is a UTF-8 encoded display name for the channel (e.g. `"Public"`, `"Team Alpha"`, etc.), padded with nulls if shorter. It’s primarily for the user interface; it doesn’t affect encryption, but it helps identify the channel. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



\*   \*\*Secret Key\*\* – 32 bytes: This is the binary secret (PSK) used for encryption on this channel. When a channel is created via the UI or CLI, a Base64 string is provided for the key; the firmware decodes it into these 32 bytes for storage. MeshCore supports 128-bit or 256-bit AES keys; a 32-byte value corresponds to a 256-bit key, but it can also operate with 16-byte (128-bit) keys if so configured. (The default Public channel key, for example, is 16 bytes = 128-bit, stored with padding in the 32-byte field.) \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



\*   \*\*Reserved/Unused\*\* – 4 bytes: At offset 0 of each record, there are 4 bytes currently marked “unused” in the file format. These might be alignment padding or reserved for future use (e.g. flags or a shorter channel ID), but as of now they are not actively used in channel operations. MeshCore simply writes zeros or ignores these bytes. The total record size becomes 4 + 32 + 32 = 68 bytes. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



\*\*In-memory representation:\*\* When the device boots, it loads the channels from `/channels2` into memory. The system uses a static array or table (MeshCore avoids dynamic allocation, in line with its design principles of fixed memory use). Each channel gets an index (0, 1, 2, …) based on its position in the file. The code uses this index to refer to channels. Notably, \*\*index 0\*\* is typically the default Public channel slot. Indexing makes channel lookup efficient – e.g., sending a message on “channel 2” means use the third record’s secret key. The DataStore system is responsible for loading and saving these records using callbacks `onChannelLoaded` and `getChannelForSave` in the firmware. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore) \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)



\*\*Capacity and Limits:\*\* By default, the channel table is set to a maximum of 40 entries on companion/terminal devices. This is more than enough for typical use (most users will use perhaps a handful of channels at once). The number “40” appears to be chosen to balance flexibility with memory constraints. In code, the channel index could technically go up to 255 (the index field is one byte), but common builds restrict it to 40 active channels for now. If a user tries to add beyond this, the operation would fail (no space). Each channel occupies 68 bytes in flash and a similar amount in RAM, so 40 channels consume about 2.7 KB of storage – quite modest. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)



\*\*Example – Default Public Channel Entry:\*\* On a fresh device, `/channels2` will contain at least one entry: the Public channel at index 0. For example, it might look like (in human-readable terms):



\*   Index 0: Name = `"Public"` (padded to 32 bytes), Secret = (binary decoding of `izOH6cXN6mrJ5e26oRXNcg==`), Reserved = 0. This entry is preloaded by the firmware (so you don’t have to manually add the public channel). The secret corresponds to the known hex key `8b3387e9c5cdea6ac9e5edbaa115cd72`. All devices share this same key for channel 0. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



Other indices are initially empty. The channel file does not write out empty slots at the end, to save space – “empty channel slots are not written to disk”. Instead, it may truncate after the last used entry. For instance, if you only have two channels configured, the file will contain two 68-byte records (136 bytes total). If you later add a 3rd channel, the file will extend by another 68 bytes at the end. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



\*\*Adding/Removing Channels:\*\* When you add a new channel via the UI or CLI, the system finds the first available index and writes a new `ChannelDetails` record to `/channels2`. Conversely, when you remove a channel, it may either blank out that slot or compress the list (the exact behavior depends on implementation; often removed channels might leave a gap that’s marked unused). Because channels are referenced by index in other contexts, MeshCore will typically \*not\* reorder the existing channels when one is removed; it might just mark that index as free. (The documentation indicates channels are stored sequentially by index and that empty slots are simply skipped on save.) \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



\*\*Lazy Write Mechanism:\*\* MeshCore uses a \*\*lazy-write\*\* strategy for both contacts and channels. When you modify the channel list (add or delete a channel), it doesn’t immediately write to flash. Instead, it marks the data as “dirty” and schedules a save a few seconds later (5 seconds is typical). If more changes occur in the interim, they’ll be batched into one write. This prevents excessive flash wear when multiple updates happen (for example, if you join several channels in quick succession). The DataStore will call `saveChannels()` after the delay to flush changes to `/channels2`. If the device reboots unexpectedly before the lazy write, there is a small chance the recent channel change might not persist (similar to unsaved contacts), but the 5-second window is short and the risk is low. \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)



\*\*Channel ID (Hash):\*\* One piece of data not explicitly stored but derived is the channel’s \*\*identifier hash\*\*. MeshCore computes a short identifier from the secret key, used in packet headers to label which channel a message belongs to. Specifically, it uses a \*\*SHA-256 hash of the secret and takes (at least) the first 2 bytes\*\* as the channel ID. This value isn’t stored in `/channels2` (the firmware can recompute it at startup for each channel). For example, if a channel’s secret hashes to bytes starting `0xAB 0xCD…`, then `ABCD` becomes the channel’s tag. This ID is what you see in an encrypted group message packet instead of a destination address. Using a short hash helps keep the packet overhead small (2 bytes vs. a full 32-byte key), while still making it unlikely that two different channels a node has will share the same ID. (With 16-bit space, collision is possible but rare given a 40 channel limit and random keys.) The `calcHash()` function in `GroupChannel` likely produces a 4-byte value for internal use (as indicated by `calcHash(): uint8\_t\[4]` in the code, of which 2 bytes are used on-air). All of this is under-the-hood; as a user or SDK developer, you typically refer to channels by name or index, not by their hash. But it’s good to know that the system has a way to tag channel messages. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood) \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage)



\*\*Storage Location:\*\* Depending on the hardware, `/channels2` may reside in different physical storage mediums:



\*   On ESP32-based devices (like T-Deck), it’s stored in the SPIFFS flash filesystem by default. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)

\*   On nRF52 or STM32, an “InternalFileSystem” abstraction is used (internal flash). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)

\*   On devices with an SD card (T-Deck, if present), MeshCore still typically uses internal flash for critical files like contacts and channels, not the SD. (SD is more for maps, etc.) However, the deepwiki hints that T-Deck Ultra firmware might also use SD for the contact/channel files if available. Regardless, the path is the same (`/channels2`). The code handles the file I/O uniformly via `DataStore` APIs.



In any case, channels persist across reboots and even firmware upgrades (as long as the upgrade doesn’t wipe the filesystem). The system is designed to migrate old storage formats if needed; for example, older MeshCore versions had different file naming (like `/node\_prefs` vs `/new\_prefs` for config). For channels, the current format is `/channels2` – if a prior version used a different format (say a hypothetical `/channels1`), the new firmware would ignore or migrate it. (The “2” in the name indicates it might be a second iteration of the format, possibly introduced around MeshOS v1.2 or similar, but the user usually doesn’t have to worry about that.) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)



\*\*Memory Usage:\*\* Once loaded, each channel occupies a small structure in RAM (similar to the 68 bytes on disk, plus maybe a few bytes overhead for pointers or state). 40 channels thus would use on the order of 2.7 KB of RAM. This is trivial on devices like ESP32 with heaps of RAM, but even on an nRF52 (which has \\~64KB RAM) it’s acceptable. MeshCore’s “zero dynamic allocation” policy means these structures are allocated as static arrays at compile time. So if only 5 channels are in use, the memory for 35 potential channels is still reserved, but it stays unused. This trade-off ensures simplicity and reliability (no fragmentation). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore)



\*\*Channel Metadata:\*\* Aside from name and key, note that \*no other metadata is stored\* for channels. For instance, there is no field for “last used time” or “created time” in `ChannelDetails`. There is also \*\*no built-in participant list or access control list\*\* stored with the channel – the system doesn’t keep track of which contacts are in a channel. The concept of “who is in the channel” is left entirely to who has the key. This means from a storage perspective, channels are extremely simple: the list of channel definitions is just a flat list of keys and labels, nothing more.



\## 4. Channel Discovery and Joining Mechanisms



Because channels serve to partition the network, a key question is: \*\*How do nodes find and join channels?\*\* In other words, how do you get a device onto the same channel as your peers? This typically involves an \*\*out-of-band sharing step\*\* – channels aren’t automatically discovered (except for the public channel which everyone has by default). Here’s how channel joining works in practice and in the MeshCore protocol:



\*\*Creating a Channel:\*\* A user (or an application using the MeshCore SDK) creates a new channel by generating a secret key and assigning a name. This can be done through the MeshCore companion app’s UI or via CLI command. For example, using the CLI one could run:



&nbsp;   set\_channel 1 "TeamChat" Xc+482JS23Lmz== 



to create a channel at index 1 named "TeamChat" with a given Base64 key. Under the hood, this calls `addChannel(name, psk)` in the device firmware, which decodes the Base64 into the binary secret and stores the entry. At this point, the channel \*exists on that device\*, but other devices won’t know about it yet. \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)



\*\*Sharing the Channel (Out-of-band):\*\* The creator must distribute the channel info (name + secret) to others who should join. MeshCore provides convenient methods for this:



\*   \*\*QR Codes / URI Links:\*\* The MeshCore mobile app or T-Deck device can output a QR code encoding the channel credentials. The format used is a URI scheme, often something like `meshcore://channel?name=<Name>\&key=<Key>` (though the exact format isn’t officially documented in public, it’s implied by similar features for contacts). For instance, an “Export Channel” action might produce a string like `MeshCore Channel: TeamChat, Key: Xc+482JS23Lmz==` or a URI that devices know how to parse. The receiving user can scan this QR with their MeshCore app or paste the URI, and the app will interpret it to add the channel. This is analogous to exchanging “contact cards” (MeshCore’s `clipboard.txt` mechanism is used for contacts and likely channels as well). In logs, these are sometimes called \*“biz cards”\* or just \*channel QR\*. \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



\*   \*\*Manual input:\*\* Alternatively, one can manually give the Base64 key and channel name to a peer (say over a secure chat or written on paper) and they can use the CLI or app UI to enter it. For example, `set\_channel 1 "TeamChat" Xc+482JS23Lmz==` on their device as well.



Once the other device imports the channel info, it will save it in its `/channels2` file at some index.



\*\*Discovery within a Private Channel:\*\* After sharing, all intended members now have the channel configured locally. But how do they find each other on that channel? Recall, if these nodes were previously strangers (not in each other’s contact list) and if they operate purely on the private channel, they weren’t exchanging public adverts. In a fully closed network scenario (no public channel use), a node might begin transmitting \*\*encrypted adverts on the private channel\*\*. MeshCore supports encrypting identity adverts with a channel key for privacy. In effect, the advert becomes a special payload on that channel that only those with the key can decrypt (revealing the sender’s public key and name). This way, the nodes in the private channel can still discover each other’s identities and add contacts, but outsiders can’t make sense of those adverts. If the channel group is small and within radio range, they might not even need to formally add contacts – they could just remain communicating via group messages. However, adding as contacts is beneficial for acknowledgments and potential direct messages, so usually discovery of peer identities will happen either via the channel adverts or by pre-sharing identities as well. \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



In practice, many users leave the Public channel enabled even while using a private channel, to facilitate initial contact exchange. A common pattern is:



1\.  Two nodes meet on the Public channel (they see each other’s adverts and automatically add each other as contacts in the list).

2\.  They decide to move to a private channel for chat. One shares a channel URI and both add it.

3\.  Now for messaging, they select that private channel in the UI and send group messages there. Since they were already contacts, they know each other’s identity and can see each other’s channel traffic right away (also, being contacts means their devices might attempt a direct route if available; but since channel messages are designated as group, they will be processed as group traffic, not as direct messages).



\*\*Channel Advertisement and Joining via Room Server:\*\* Another mechanism is via \*\*Room Servers\*\* (if present). A Room Server is a node that stores messages and might assist in discovery. Some room server configurations could advertise available channels or require a password to join (the FAQ hints at “What is the password to join a room server?” which suggests some access mechanism). However, room servers in MeshCore are more about storing message history than advertising channels. Channels themselves are not like chat rooms you join via server; they are more decentralized. So in MeshCore, joining a channel is fundamentally about obtaining the key from someone who already has it – there’s no central directory of channels on the mesh. \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ)



\*\*meshcore-cli and Channel Listing:\*\* On a technical note, one can query and manage channels via the CLI. For example, running `get\_channels` will list all channels on the device with their index, name, and key info. If a user has just been given a channel key, they can use `set\_channel` to add it. The CLI even has a `public` shorthand to send messages on channel 0 and a `chan` command to send to a specified channel index. This indicates that under the hood channels are referenced by index number. For the .NET SDK, similar functionality would be available – e.g., APIs to create a channel (specifying a name and key), delete a channel, list channels, etc. \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



\*\*Joining Workflow Recap:\*\* Summarizing how one would join a channel:



1\.  \*\*Obtain the Channel Secret:\*\* via QR code, link, or manual sharing. This typically provides both the key and the intended channel name.

2\.  \*\*Add Channel to Device:\*\* using the companion app or SDK call – the device stores the new channel (name \& key) in `/channels2`.

3\.  \*\*Start using Channel:\*\* If the channel is meant to be the only network (like a closed group), the user might disable the public channel or at least switch their UI to send on the private channel. Now they can send a message on that channel.

4\.  \*\*Discovery among Channel Members:\*\* If not already contacts, members will start seeing each other’s channel messages or encrypted adverts. They can then add each other as contacts if needed (MeshCore will automatically create a contact entry when it sees a valid message or advert from an unknown identity, provided it can decrypt it and “trusts” it – typically, you need to explicitly add a contact unless an advert is detected; by default unknown messages may be ignored to prevent spam). \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



It’s worth noting that \*\*channel secrets are the “credential” to join\*\*. There is no separate concept of a channel password vs. channel key – they are one and the same. Knowing the 32-byte secret \*is\* what grants access. There’s no additional authentication within the channel. So sharing that secret out-of-band must be done securely. The security model assumes the channel secret is distributed only to trusted parties. If it leaked, an unwanted party could silently join (there’s no built-in way to detect that directly, except noticing unfamiliar messages/names appear).



\*\*meshcore://channel URI Format:\*\* While not formally documented in available materials, evidence suggests a URI like `meshcore://` is used for export/import. The T-Deck’s “Card to Clipboard” feature writes a text file with lines starting with `meshcore://...` for contacts. By analogy, a channel share might look like: \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



&nbsp;   meshcore://channel/TeamChat?key=Xc%2B482JS23Lmz%3D%3D



(This example is speculative, showing a URI with a channel name and a URL-encoded Base64 key.) The receiving app would parse that and call the appropriate API to set the channel. The enterprise analysis indeed references a “MeshCore’s ‘Channel: secret’ QR format” being used to create closed meshes. So as an SDK developer, if you want to implement channel sharing, you might follow this convention: encode channel name and key in a URI or QR that others can scan. \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



\*\*Channel Discovery vs Contact Discovery:\*\* It’s important to separate the two concepts. \*\*Contact discovery\*\* (finding nodes) is usually via adverts on whichever channel(s) the node is using for advert broadcasts. \*\*Channel discovery\*\* (finding channels) as a separate concept doesn’t really exist – channels are not advertised by name or something. You won’t hear “Channel X is available” on the mesh. Instead, you hear nodes (contacts) advertising, and inferred from their advert you might deduce if they share a channel with you if, say, you can decrypt some info. However, since adverts can include an encrypted blob when on a private channel, a node that does not have the channel key will just see gibberish or nothing meaningful. So effectively, if you don’t already know the channel, you won’t discover it spontaneously. You have to be told about it. This design prevents random people from learning that a private channel even exists, increasing security.



\*\*Role of Repeaters in Joining:\*\* Repeaters do not have any special knowledge of channels (unless configured). A repeater will forward any packets it hears (public or private) as long as they are valid mesh packets. It does not need to decrypt them to repeat them. So if two users share a private channel key but are out of direct range, a standard MeshCore repeater (which might only know the public channel) can still pass along their private channel messages blindly. However, for best results in a private mesh, usually repeaters are also configured with the channel if possible. In practice, a repeater can be given the same channel key so that it will also send out adverts on that channel and fully integrate into that network. If not, it will still flood-forward the traffic due to the flood routing approach, but it might not generate its own adverts for that channel, etc. So, when setting up a large private mesh, one should configure repeaters with the private channel as well (MeshCore repeaters support multiple profiles, but that’s beyond our scope).



In summary, \*\*joining a channel is a manual but straightforward process\*\*: get the key from someone, add it to your device, and you’re in. There is no complex handshake or central server to approve membership – possession of the key is the only requirement. From a developer’s perspective, implementing channel join in the .NET SDK means providing functions to input a channel name and key (or scan a QR code) and then calling the MeshCore device (via serial, BLE, etc.) to set that channel. The SDK can also generate QR codes for sharing channels (since the format is known). Once joined, the SDK can listen for messages on that channel or send to it by referencing it (likely by name or index).



\## 5. Security and Encryption in Channels



Security is a major aspect of the MeshCore channel design. Channels introduce an \*\*additional layer of cryptography\*\* on top of the mesh’s base encryption between known contacts. Let’s break down how encryption works for channel communications and how MeshCore manages keys:



\*\*Channel Secret Key:\*\* Every channel has a secret key (PSK) as discussed, which is essentially an AES key. MeshCore supports using that key at either 128-bit or 256-bit strength. The default Public channel uses a 128-bit key (16 bytes), whereas when you create a custom channel via the app, it often generates a 256-bit key (32 bytes) for maximum security. In either case, the key is stored internally as a 32-byte array (if the key is 16 bytes, I suspect the remaining 16 bytes might be zero or derived somehow, or the software might treat it as 256-bit either way – the documentation explicitly notes 128 or 256-bit are supported). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)



\*\*Encryption Algorithm:\*\* MeshCore employs \*\*AES\*\* for encrypting messages. The exact mode of AES isn’t stated outright in the fragments we have, but given the separate mention of “Message MAC (authentication)”, it’s likely using AES in CBC or CTR mode plus an HMAC-SHA256 for integrity, or possibly AES-GCM for authenticated encryption. However, since they mention a 4-byte truncated SHA256 for ACKs and an explicit “MAC” field in the packet, it’s plausible they use AES-CTR or AES-CBC and then attach a SHA256-based MAC. Regardless, for our purposes: a channel message payload is \*\*encrypted with the channel’s AES key\*\*, and a \*\*MAC\*\* is included to ensure it hasn’t been tampered with. The encryption covers the message content (and timestamp), but not the routing header (which contains things like channel ID and possibly sender/target info if any). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



\*\*End-to-End Encryption:\*\* Channel messages are encrypted end-to-end \*with the group\*. This means that if you send a message on a private channel, only devices that have that channel’s key can decrypt it. Repeaters in between do not decrypt it (they don’t need to and often don’t have the key). Thus, even multi-hop transmissions remain encrypted over each hop. This is true for direct messages as well (those use per-contact shared secrets). So MeshCore’s encryption sits at the application layer above routing, giving true e2e security.



One subtle point: direct contacts use \*pairwise\* shared secrets (derived by ECDH from each node’s Ed25519 keypair). This provides \*perfect forward secrecy\* for one-to-one chats (since each pair’s secret is unique, and if one contact is removed or keys rotated, it doesn’t affect others). Channels, however, use a \*static group key\*. This is more akin to a pre-shared WiFi password – it does not automatically refresh or provide forward secrecy. If the key is compromised at any point, theoretically past and future communications with that key are compromised until it’s changed. This is the trade-off of ease-of-use vs. cryptographic strength. Within a channel, there’s no per-sender key derivation; everyone uses the same symmetric key. Therefore, \*\*channel messages are not individually signed by the sender’s identity\*\*. In fact, the MeshCore FAQ calls group messages “unverified”, meaning that \*anyone with the channel key could have authored a given message\*. The system does include the sender’s \*\*display name\*\* in the encrypted payload of a group text (`"name: msg"` format), but there isn’t a cryptographic signature to prove it truly came from that person. All members trust each other by virtue of sharing the key. This is important to note: while channel encryption keeps outsiders out, it does not provide non-repudiation or intra-group authentication. (If Alice and Bob share a channel key, Bob could theoretically impersonate Alice by constructing a message that says “Alice: ...” if he wanted, and the device would display it as from Alice because it decrypted properly with the channel key. There’s no digital signature tied to Alice’s Ed25519 in that scenario.) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



To summarize that: \*\*within the channel, trust is assumed\*\*. All participants are trusted not to spoof each other. The design keeps the protocol simpler (no need to manage per-message signatures from each member). This is somewhat similar to how a two-way radio channel works – anyone on the frequency can speak and claim to be whoever, and others rely on voice recognition; here it’s text and name. This is usually acceptable for small private groups who have exchanged the key out-of-band (because if you gave someone the key, you presumably trust them not to misuse it). But it’s a conscious design trade-off.



\*\*Preventing Unauthorized Access:\*\* The primary mechanism to prevent unauthorized access to channel communications is simply that \*\*only nodes with the correct secret can decrypt\*\*. If a node doesn’t have the channel configured, any channel message appears as random noise (they can see a packet came through, but it will have a channel hash they don’t recognize and the payload will not make sense). Additionally, as mentioned, if a device isn’t on the channel, it likely won’t even add the sender as a contact. MeshCore by default \*\*ignores messages from unknown contacts\*\* to avoid spam or malicious injection. So even if an attacker somehow got a message onto the mesh on that channel, if the receiving device doesn’t already have some trust (like either the channel or the sender’s contact), it might drop it. However, if the attacker has the key, they’re effectively an “insider” and the system will treat them as part of the group – there is no further access control. Thus, the key must remain secret. \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1)



There is no separate password or PIN beyond the key itself. In repeater or room server contexts, there are admin passwords (like for logging into a repeater’s console, or to join a room server as client, a password may be required). But that’s a different layer. For channels on client devices, \*\*the channel key is the one and only gatekeeper\*\*. \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ)



\*\*Encryption in transit:\*\* Let’s illustrate what happens when a text message is sent on a channel, e.g. "Hello Team" on channel "TeamChat":



\*   The sending device takes the plaintext `"Hello Team"`, prefixes it with the sender’s name (say the sender’s contact name is “Alice”), forming `"Alice: Hello Team"`, and perhaps a timestamp. It then encrypts this data using AES with the channel’s secret key. The output is ciphertext. \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)

\*   The device then constructs a packet with:

&nbsp;   \*   \*\*Payload Type\*\* = `PAYLOAD\_TYPE\_GRP\_TXT` (0x05) indicating a group text message. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)

&nbsp;   \*   \*\*Channel Hash (ID)\*\* = the first 2 bytes of SHA256(secret) e.g. `ABCD` (this goes into the header to label the channel). \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)

&nbsp;   \*   \*\*Source and dest fields:\*\* For a group message, there is no single destination. In MeshCore’s packet format, instead of dest/src hash for unicast, they put the channel hash and perhaps the sender’s hash or leave one of them as channel. The code snippet suggests “prefixed with channel hash, MAC” which implies the 4-byte channel hash and then a MAC, but doesn’t explicitly include sender hash. Possibly the `src\_hash` still contains the sender’s 4-byte ID (public key hash prefix). If so, receivers who have that contact will know who it is. But even if the sender’s ID is included, since the message isn’t signed, it’s not guaranteed – but it’s likely included for routing/response purposes or just for info. (We do know the advert system uses 4-byte pubkey hashes as addresses.) \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)

&nbsp;   \*   \*\*MAC\*\* = a cryptographic message authentication code, likely 4 or 8 bytes, calculated over the encrypted payload and some header fields. This ensures the message wasn’t corrupted or forged without the key. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)

\*   This packet is then broadcast. All repeaters that hear it will treat it as a normal mesh packet. They see a new packet with type 0x05 and some channel ID in the header. They don’t know what that channel is (unless they coincidentally have the same key configured, which likely they don’t if it’s private), but they will forward it because they forward anything that’s not obviously invalid or duplicate (repeaters operate largely at layer 2/3, not needing to parse application payload). Each repeater will retransmit it once (flood mode). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)

\*   Each end device that has the channel will receive the packet. They look at the channel hash prefix. They compare it to the hashes of the channel keys they have configured. If it matches one (e.g. “TeamChat” key yields `ABCD` which matches), they know which key to use to decrypt. They verify the MAC (to ensure the ciphertext hasn’t been altered and that the key is correct – if the key were wrong, the MAC check would fail). If valid, they decrypt the payload with the channel secret. Now they get `"Alice: Hello Team"`, which they display in the UI under the channel’s chat. If the device has a contact named Alice (public key matching the source, or matching the name in plaintext and context), it might tag the message as from Alice. Otherwise it may just show the raw name string from the message. The message is thus delivered.

\*   A device without the channel key either drops the packet or stores it as an unknown blob. It cannot decrypt it. Typically, it would just ignore it (and likely the MAC check fails since it can’t compute the right MAC, so it knows this message is not for any channel it knows; the mesh library would discard it).



\*\*Key Generation and Distribution:\*\* When you create a new channel via UI, the software likely generates a random 256-bit number (MeshCore has an RNG seeded from radio noise entropy for cryptographic use). Earlier MeshCore versions had an entropy issue in version 1.0.8, but that applied to Ed25519 key generation; presumably channel keys are fine now (but one should always use latest firmware to ensure good entropy). The Base64 string is usually just an encoding of the raw key bytes. Users can also choose a custom key (some apps let you type a passphrase which is then hashed into a key, though that’s less common in MeshCore’s provided tools – usually it just gives you a random one).



\*\*Key Storage:\*\* Channel keys are stored in flash (in `/channels2`) unencrypted, but the flash itself is not publicly accessible without physical device access. If someone stole your device, they could extract the channel keys by reading the memory, so physical security matters. However, within the running device, they remain in memory as needed. There is no key exchange protocol for channels – it’s all pre-shared. If a channel’s secrecy is compromised or you want to \*\*remove a member\*\*, you have to manually change the key (i.e., create a new channel or update the secret and redistribute it to the remaining members). There’s no concept of kicking someone out remotely; you’d just stop sharing the new key with them.



\*\*Comparison to Direct Contact Encryption:\*\* MeshCore’s direct messages use an \*\*ECDH-derived shared secret per contact\*\* (32 bytes stored in `ContactInfo.shared\_secret` for each contact). Those are established through the exchange of Ed25519 public keys in adverts and computing an X25519 (Diffie-Hellman) secret. This gives each pair of nodes a unique key that is never transmitted – it’s computed independently by both after exchanging pub keys. Channel keys, by contrast, do not use public-private crypto at all – they are symmetric and must be explicitly shared. So from a \*\*cryptographic strength\*\* perspective: \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage), \[\\\[deepwiki.com\\]](https://deepwiki.com/meshcore-dev/MeshCore/8.4-contact-and-channel-storage) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



\*   Direct contact encryption has the advantage that even if someone intercepts all your traffic, they cannot derive the shared secret without your private keys; and if one contact’s key is compromised it doesn’t affect others. It also authenticates the sender because each message is encrypted with a key that only two parties know.

\*   Channel encryption means any member could theoretically produce any message. It doesn’t provide sender authenticity or sub-group keys. But it significantly simplifies group messaging. Many secure messengers (like Signal) solve this with complex group protocols with per-member keys, but in a resource-constrained mesh device that’s not implemented.



\*\*Security Best Practices for Channels:\*\*



\*   Always use \*\*private channels\*\* (custom keys) for any communications you don’t want outsiders to read. The public channel’s key is public knowledge. It’s fine for coordination or trivial chat, but it’s not secure if an adversary is likely to be listening. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)

\*   Keep channel keys secret. Share them only by secure means. For example, if you’re using MeshCore in the field, you might generate a QR code from one device and have teammates scan it directly (so no key goes over an insecure channel like email).

\*   If a channel key might have been compromised (e.g. a device with the key was lost to an adversary), \*\*retire that channel and create a new one with a new key\*\*. There is no way to “change” the key on the fly; you’d treat it as a new channel and inform your team. All old traffic remains encrypted (the adversary would have to get the lost device’s memory to read past messages, unless those messages were also stored on a room server– more on that in a bit).

\*   Understand that channels don’t authenticate the sender. If in doubt, verify important messages through another channel or via context. (In practice, impersonation has not been flagged as a serious issue in user forums, likely because channel groups tend to be small and known.)



\*\*Encrypted Adverts and Privacy:\*\* One huge benefit of private channels is that they allow you to operate a \*\*closed mesh network\*\*. When all devices use a private channel for their adverts and messaging, \*\*outsiders will not even see your node names or existence\*\* (except perhaps as un-decryptable packets). The enterprise analysis notes: “you can create a closed mesh where only invited nodes (with the channel secret) can join and discover each other. This means you won’t even appear as a contact to someone not on your channel.”. This is a key privacy feature. By contrast, on the public channel, your device is shouting “Hi I’m Alice (public key xyz)” in the clear to anyone listening. In scenarios where anonymity or stealth is desired (e.g. activists or military scenarios), using a private channel and disabling public adverts is crucial. \[\\\[MeshCore C...tem Techni | Word\\]](https://microsoft-my.sharepoint.com/personal/wayneb\_microsoft\_com/\_layouts/15/Doc.aspx?sourcedoc=%7BC8A9C280-FB5F-4DEB-BBCB-B505CE5693C2%7D\&file=MeshCore%20Contact%20System%20Techni.docx\&action=default\&mobileredirect=true\&DefaultItemOpen=1) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



MeshCore’s approach here is similar to Meshtastic’s channel isolation: devices on different channel keys are effectively invisible to each other, even if they share frequencies. They might still cause RF interference, but they ignore each other’s content.



\*\*Room Servers and Channel Security:\*\* A MeshCore \*\*Room Server\*\* is like a bulletin board that stores messages (like a history log) and allows clients to sync with it. When a Room Server stores channel messages, those messages are likely \*stored encrypted\*. However, since the Room Server might want to index them or allow retrieval by users who come online later, it actually has to have access to the plaintext. This suggests that a room server must possess the channel key (or at least a way to decrypt the messages for storage). If you trust a room server with your communications, you share the channel key with it (like another member of the channel). There is an ACL system (ClientACL) that can restrict who can retrieve messages from the server, often with an admin controlling a password for clients to connect. However, diving deeper into room server is beyond scope – just note that introducing a room server into a channel essentially is like adding another participant (with potential special privileges to retain messages). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9-security-and-access-control), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9-security-and-access-control)



\*\*Encryption Summary:\*\* All in all, channel encryption in MeshCore provides a robust barrier against outsiders and a convenient way to secure group messages, with the trade-off that \*within\* the group, trust is communal. The cryptography used (Ed25519, X25519, AES, SHA256) is modern and strong. The design’s simplicity is intentional given the constraints of LoRa bandwidth and device CPU – it opts for a shared symmetric key model to avoid the overhead of multi-party key agreement in mesh conditions. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9-security-and-access-control), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



\## 6. Channel Communication Protocol and Routing



Once channels are set up and keys exchanged, how do communications actually flow? We’ve touched on this in parts above, but here we’ll focus on the \*\*operational characteristics\*\* of messaging within channels: how messages are delivered across the mesh, how reliability is handled, and what happens if some members are offline.



\*\*Message Flow in a Channel:\*\* In MeshCore, a \*channel message\* (whether text or data) is treated as a broadcast to the channel participants. There is no explicit list of recipients in the packet; instead, the channel itself is the “address.” Under the hood, channel messages typically use \*\*flood routing\*\* by default: \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)



\*   The originating node sends the message with `ROUTE\_TYPE\_FLOOD` (this is often the case when the destination is unspecified). This means all repeaters that hear it will retransmit it, as long as they haven’t seen the same packet before. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)

\*   MeshCore repeaters implement flooding carefully: each packet has a unique hash (based on contents and sequence number), and repeaters keep a short-term cache (MeshTable) to avoid rebroadcasting duplicates. So the message will propagate outward through all repeater nodes exactly once per repeater, covering the network. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)

\*   If the network is connected (everyone is within some hops of the source), the flood will reach all nodes. In a large network with multiple repeaters, flood routing ensures broad delivery at the cost of potentially more transmissions than necessary (since not every node needs it, but flood doesn’t discriminate). MeshCore does have path-learning optimizations for unicast, but for group messages, flooding is the straightforward approach.

\*   After an initial flood, nodes might learn routes. However, for group traffic, direct routing isn’t as straightforward because you’d need a route to each member. MeshCore doesn’t currently implement a multicast routing protocol with dynamic membership. Instead, flooding is the safe method to reach all. (If a particular node is known to be reachable via a certain repeater path, one could envision a system where the sender only floods to those repeaters or uses multiple directed sends. But given the relatively low traffic of typical usage, flood is acceptable.)



\*\*Repeaters’ Role:\*\* As mentioned, repeaters forward channel traffic just like any other mesh packet (provided it’s marked for flooding or if they have “store \& forward” roles like a room server). \*\*Crucially, repeaters do not need to decrypt channel messages to forward them\*\* – they operate at the packet level. Even adverts or group messages they can’t decrypt, they will still flood if flood routing is used, because the check is based on packet hash. This means a repeater can assist in delivering private channel messages even if the repeater doesn’t itself share that channel key.



However, if the repeater does not share the channel, it will treat those packets as “opaque.” It can’t interpret them beyond what’s in the header. This might limit certain features: e.g., if a repeater wanted to monitor traffic volume per channel, it can only identify channel by hash, not by name (and it might not even know the name if it doesn’t have the key configured). Usually that’s not needed.



\*\*Offline Recipients and Persistence:\*\* The base mesh (without a room server) is essentially an \*ad-hoc, real-time network\*. If you send a channel message while some intended recipients are offline or out of range, those recipients will \*\*miss the message\*\*. There’s no automatic store-and-forward per recipient. For direct messages, MeshCore implements an acknowledgment system (for reliability) but not storage. For group messages:



\*   \*\*Acknowledgments:\*\* MeshCore’s ACK packets (PAYLOAD\\\_TYPE\\\_ACK 0x03) are used for direct unicast messages. For group messages (0x05/0x06), acknowledgments are tricky since multiple nodes would have to ack to the sender. As far as available info, it appears MeshCore does \*not require ACKs for group messages\*. The docs explicitly list ACK only for direct TXT and REQ messages. A group text is flagged as “unverified” implying no per-recipient verification or ack. So, group messages are sent \*\*unreliably\*\* – similar to UDP broadcast. The trade-off is that adding ACKs for each receiver would massively increase traffic in a dense group. Instead, if reliability is needed, a higher-level protocol or human confirmation is used (e.g., if it’s critical, someone can reply “Got it” manually). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)



\*   The \*\*lack of ACKs\*\* means senders do not know for sure who received a group message. They fire-and-forget. In practice, LoRa has limited throughput, so one keeps messages short and perhaps repeats important ones manually.



\*   \*\*Room Server for Persistence:\*\* If using a Room Server node, the dynamic changes. A room server (advertised as `ADV\_TYPE\_ROOM`) can serve as a repository. Clients can connect to it and sync any messages they missed while offline. Essentially, the room server plays the role of a bulletin board that retains channel traffic (it’s like a mailbox or forum that stores everything until fetched). If a node goes offline and later returns, it can request missed messages from the room server. This requires that:

&nbsp;   \*   The room server is configured to subscribe to the channel (likely by having the channel key and being present in the mesh or at least getting the messages).

&nbsp;   \*   The client knows to query the room server for history (MeshCore likely does this when connecting to a room server).

&nbsp;   This is how “offline delivery” is achieved in MeshCore – not peer-to-peer, but via an optional infrastructure node. The concept is akin to an email server or BBS in the mesh. Of course, using a room server implies you trust it with the plaintext (thus often these are deployed within a closed group anyway, effectively an extension of that group’s trust).



\*   Without a room server, \*\*messages are transient\*\*. Devices themselves do not retransmit old messages except as immediate repeats if no ack (and for group, we don’t have ack, so likely no automatic retries). If a message collides or a node’s radio is busy and misses it, that message might be lost. Mesh networks like this typically accept some packet loss.



\*\*Concurrent Channels:\*\* A single device can technically listen on multiple channels at once (since it’s the same radio receiving all packets, it will just attempt decryption on any it recognizes). The MeshCore library likely iterates over known channels to try to decrypt relevant incoming group packets. So a device in channels A, B, and C will handle all messages for A, B, C. This is concurrent logically. However, \*\*transmission\*\* is one channel at a time – when you send, you specify which channel context to use (e.g., user selects channel in UI). There’s no notion of “tagging” a single message to multiple channels; you’d have to send separate messages on each channel if you wanted to broadcast to two different groups.



\*\*Routing Efficiency:\*\* MeshCore implements path learning to optimize unicast transmissions (using discovered routes in `ContactInfo.out\_path`). For a channel message, there is no single target to route to, so flooding is default. There is a curious possibility: if a channel is very large and widely dispersed, flooding might be inefficient. One could conceive that MeshCore might use a technique where if it has a direct route to some subset of members, it could send copies of the message along each route (like unicast to certain regions). However, that’s not mentioned in documentation and would complicate things. It’s more likely that for now, flooding is always used for group messages, and any route info in the packet header isn’t utilized for multi-hop beyond standard flood behavior. (I.e., `packet->path` might be empty or just contain the sender’s hop for reverse path learning, but not used to direct group messages.) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)



\*\*Interaction with Repeater Whitelisting:\*\* In the GitHub issue snippet, we saw repeaters can have a whitelist and only add those repeaters when they “advert” normally. That was about repeaters advertising themselves (not channel-specific). There’s no evidence of channels affecting repeater behavior, aside from the general point that if a repeater doesn’t share a channel key, it still forwards but won’t list those nodes in its contacts. \[\\\[github.com\\]](https://github.com/meshcore-dev/MeshCore/issues/1419)



\*\*Multi-hop Example:\*\* Suppose we have a line of three nodes A, B, C in a row, with B as a repeater. All are on a private channel “RescueNet”. A sends a message “Meet at point X” on RescueNet. A’s message is encrypted with RescueNet key. B (repeater) hears it. Even if B doesn’t have the key (imagine B is just a generic repeater), B will flood it because it’s a new packet. C hears B’s retransmission and, having the key, decrypts the message. If C was turned off at that moment, it misses it. If later C comes on and connects to a room server that B also fed the message to, it might retrieve it.



\*\*Scalability:\*\* Channel communication scales reasonably well for moderate network sizes. If you had a \*\*very large mesh\*\* with many repeaters and many channels in use:



\*   The airtime is shared. All channels use the same LoRa frequencies set in NodePrefs (MeshCore can be configured with certain frequency and RF settings profiles – e.g., everyone might be on 915 MHz SF7). Channels are logical and do not divide the spectrum. So if multiple groups (channels) are chatting in the same area, their packets will collide just as if they were on one channel. The only benefit of separate channel keys in that situation is that they won’t waste time trying to decrypt others’ messages, but the RF medium doesn’t care – a collision is a collision. This means \*\*channels do not mitigate RF congestion\*\*; they’re for logical segmentation and security, not FDMA/TDMA. To reduce RF interference between separate user groups, you’d actually configure different frequency channels or spreading factors (which is a different concept, often called “modem channels” or simply separate network frequency).

\*   If a device is in multiple channels, it has to handle all corresponding traffic. A slow device might get overwhelmed if two channels are extremely busy at once, but given LoRa’s low throughput it’s likely fine.

\*   MeshCore likely processes incoming packets sequentially. If a ton of channel messages come in, its buffer or blob store could fill up. But again, the low data rate (a few packets per minute typically) means it’s rarely an issue.

\*   \*\*Total membership\*\*: If 50 devices are all in one channel and one sends a message, that’s 49 receivers – which is fine. If 50 devices are each in distinct channels with maybe a couple per channel, that’s also fine. The main limit is the number of channels in the device (40) and the radio bandwidth.



\*\*Power/Battery Impact:\*\* Listening on a channel means keeping the radio on to receive packets. MeshCore devices (like T-Beam or T-Deck) tend to keep the radio listening continuously (unless in a low-power mode). Being in more channels doesn’t make you consume more power in the radio sense – the radio is either listening or it’s off. However, if more channels means you end up getting more traffic addressed to you (because you’re effectively participating in more conversations), then you will spend more power decoding and possibly transmitting acks or replies. But being configured for 10 channels versus 1 channel has negligible idle cost difference, since all that changes is whether you can decrypt what you hear. The device isn't polling each channel separately – it’s one stream of RF.



On transmit, sending a message on a channel uses the LoRa radio for that packet, same cost as any similar-sized packet. Public channel or private channel doesn’t change that. The payload lengths might differ slightly if names are included, etc., but that’s minor. Repeating others' messages does cost power, but repeaters are usually plugged in or have bigger batteries for that reason.



\*\*Packet Structure Recap:\*\* For completeness, here’s a typical \*\*group text packet format\*\* (reconstructed from docs and code):



&nbsp;   \[ Mesh Header (1 byte type + flags) ]

&nbsp;   \[ Routing Header: Hop limit, maybe path, etc. ]

&nbsp;   \[ dest\_hash (4 bytes) + src\_hash (4 bytes) OR channel\_hash + MAC?? ]

&nbsp;   \[ MAC (e.g. 4 bytes) if not included above ]

&nbsp;   \[ Encrypted Payload: timestamp (4 bytes) + "SenderName: Message text" + optional fields ]



From the T-Deck diagnostics snippet, we see an example decoding:



&nbsp;   t:5 snr:... rssi:...  # where t:5 indicates PAYLOAD\_TYPE\_GRP\_TXT (0x05)



and it shows group text includes name: msg inside. \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood)



\*\*Keep-Alive and Heartbeats:\*\* MeshCore periodically sends `PAYLOAD\_TYPE\_ADVERT` (0x04) packets as heartbeats announcing your presence. On the public channel, these are in plaintext (signed but not encrypted) and include your public key, name, and possibly location if enabled. On a private channel, adverts can be encrypted with the channel key for privacy (or some fields hidden). These adverts act as keep-alives as well (to let others know you’re still around and to refresh routing). They are small and infrequent (e.g., every 60 seconds by default, configurable). Channel membership can influence keep-alives: if you are only on a private channel, you might send adverts only on that channel. If you have both public and private, you might send on both (the code might do dual advertising, or possibly just public – but if you enabled “private channel only” mode, then public adverts would be suppressed). The NodePrefs likely has a setting like `manual\_add\_contacts` or `advertise on main channel only` etc. The snippet shows `manual\_add\_contacts` flag which if true might require manual approval to add contacts (for private usage). \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ#55-q-do-public-channels-always-flood-do-private-channels-always-flood) \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/7-data-persistence-and-configuration)



\*\*One vs Multiple channels concurrently:\*\* A user can definitely be in multiple channels. For example, you could be on the global “All” channel and also in a team channel. You might monitor both. The T-Deck UI presumably allows switching the view between channels (similar to selecting a chat room). Internally, the device still listens for all. It might ring a notification sound if a message comes on any channel. The user could respond in the appropriate channel context.



\*\*Communication with devices not in channel:\*\* If you attempt to message a contact via a channel they are not part of, they simply won’t see it. For example, if Bob sends on “TeamChat” and Alice’s device doesn’t have TeamChat configured, Alice’s device will drop that packet (unknown channel). So effectively it’s as if Bob whispered on a frequency Alice isn’t tuned to – she won’t know it happened. If Bob also wanted Alice to get it, he’d need to either send it on a channel Alice has or directly to Alice. This is an important note: a MeshCore device can participate in multiple channels, but sending a message is always tied to one channel or a direct peer.



\*\*Direct vs Channel Routing Context:\*\* If you have a contact in your list, you can send a direct message (payload type 0x02) which will be encrypted with your pairwise key and addressed specifically to them (with their 4-byte hash as dest). That can travel more efficiently if a path is known. Channel messages (0x05) always use flood. So interestingly, for two people who have a direct contact, sending one-to-one through the channel is \*less\* efficient (it floods) than sending a direct message (which after initial flood can go direct). So you wouldn’t normally DM someone via the channel unless you explicitly wanted the message to be visible to others in the channel. In practice, one might use the channel for group discussions and use direct messages for side conversations with one person.



\*\*Interoperability with Meshtastic or other systems:\*\* It’s worth noting channels are a MeshCore concept similar to (but not the same as) Meshtastic channels. They are not interoperable – a MeshCore device cannot directly communicate with a Meshtastic device, for example, because the protocols differ. The mention in search results of “MeshCore as an alternative to Meshtastic” indicates they have separate channel configuration systems. The `meshcore-dev` docs and code govern this.



In summary, the \*\*operational characteristics\*\* of channel communication are:



\*   Flood-based delivery (no per-recipient routing optimization).

\*   No delivery guarantees (unless using additional infrastructure).

\*   Real-time only, unless using storage nodes.

\*   The mesh network (repeaters) ensures multi-hop reach, but if network segmented or a node is turned off, messages don’t queue for them (again, unless a room server is employed).

\*   Simultaneous use of multiple channels is supported, but radio bandwidth is shared.



\## 7. Channel Management Operations (CLI Commands and SDK API)



MeshCore provides command-line and programmatic interfaces to manage channels. These allow users (or developers writing an SDK) to create new channels, edit existing ones, list them, and delete them. We’ll outline the common operations:



\*   \*\*Listing Channels:\*\* The CLI command `get\_channels` prints all channel entries stored on the device. For each channel it typically shows the index number, the name, and possibly the Base64 key (the CLI might mask or show it depending on security considerations). For example, after adding some channels, `get\_channels` might output: \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



&nbsp;       0: Public  (key: izOH6cXN6mrJ5e26oRXNcg==)

&nbsp;       1: TeamChat (key: Xc+482JS23Lmz==)

&nbsp;       2: Emergency (key: abcd1234...)



&nbsp;   This helps confirm which channels are set.



\*   \*\*Query a Specific Channel:\*\* `get\_channel <index-or-name>` shows details of one channel. Using the name or number is convenient. The output could include the channel’s computed hash ID, or other info like whether it’s currently the “active” channel for sending (one channel could be marked default in UI). \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



\*   \*\*Adding/Editing a Channel:\*\* The command `set\_channel <n> <name> <key>` is used to add or change a channel. If `n` (index) is a free slot equal to current count, this adds a new channel in that slot. If `n` is an existing index, it will modify that channel’s name/key to the provided values. For instance: \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



&nbsp;   \*   `set\_channel 2 Emergency abcd1234EFGH...` – this would set channel index 2’s name to "Emergency" and key to the provided Base64 (here an example string). If index 2 was empty before, it’s now created; if it had something, that is overwritten.



&nbsp;   Internally, the device will decode the Base64 string into 32 bytes and save them. It also likely computes the new hash (for identification) at this time.



&nbsp;   The CLI usage implies that even removing a channel might be done via `set\_channel` by setting an empty key or certain flag, but there is a separate remove command for clarity.



\*   \*\*Removing a Channel:\*\* The command `remove\_channel <index-or-name>` deletes a channel from the list. If you remove one in the middle, as mentioned, it might leave a gap or renumber subsequent channels. The CLI suggests you can use name or index interchangeably (name must be unique for that to work). For example: `remove\_channel Emergency` would delete the "Emergency" channel. After removal, the device will no longer listen on that channel’s secret, and it will free up that index for reuse. \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



\*   \*\*Selecting Active Channel:\*\* Not exactly in the snippet above, but the CLI likely has a concept of selecting a channel for sending. The UI on devices definitely does (you choose which chat you’re typing into). On CLI, they have `chat\_to <contact>` and `chat` (interactive mode), but for channels, they use the direct `chan` send command. Possibly you can do something like `chat #TeamChat` to switch to that channel in interactive mode, or a command like `scope` or similar to set the default. The snippet shows a command `scope ~~: sets node's flood scope` which might be unrelated or related to area filtering. That likely pertains to repeater operation, not selecting channel. \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



&nbsp;   Instead, the CLI provides:



&nbsp;   \*   `public <message>` to send a message on the public channel (alias for channel 0). \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)

&nbsp;   \*   `chan <n> <message>` to send a message on channel number n. (They used `ch` as alias in the snippet: likely `chan` or `channel`.) \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



&nbsp;   For example: `chan 1 Hello team` would send "Hello team" over channel index 1 (TeamChat in our example).



&nbsp;   These commands make it easy to script messages to channels. The CLI's interactive mode might not currently support multi-channel chat except via these commands.



\*   \*\*Export/Import Channel via CLI:\*\* There isn't an explicit `export\_channel` or `import\_channel` in the snippet, but there are analogous ones for contacts (`export\_contact`, `import\_contact`). It's possible that sharing a channel is intended to be done via the same mechanism as contacts by generating a URI card. There is a `card` command that “exports this node URI” – that’s more for identity. For channels, one might manually give out the base64 key or rely on the app’s QR code functionality. The CLI doesn’t show a direct channel export, but implementing one in the SDK would be straightforward: you have the name and key, you can produce a `meshcore://channel...` link as assumed. \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



\*   \*\*Clearing all channels:\*\* Not explicitly shown, but one could presumably remove channels one by one or reinitialize the DataStore if needed (less common).



From an \*\*SDK perspective\*\*, one would use the MeshCore API to do the same:



\*   In code, channels may be manipulated via functions in `BaseChatMesh` class. The deepwiki mentions `BaseChatMesh.cpp` lines 731-747 have the channel addition logic. So likely functions like `BaseChatMesh::addChannel(const char\* name, const char\* psk\_base64)` exist, as well as `removeChannel(index)` etc. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/1.2-key-concepts-and-terminology)

\*   The .NET SDK could call these via a command interface. If the .NET SDK communicates with a companion device, it might actually just send the CLI commands or equivalent binary commands to achieve this.



\*\*Administrator Rights:\*\* There’s no special admin for channels on the device – any user with physical or authorized access to the device can add/remove channels on that device. In a multi-user sense, channels don’t have an “owner” in the mesh protocol. However, in practice the person who created and distributed the key is effectively the admin outside of the system (they control membership by deciding who gets the key).



\*\*Permissions:\*\* The MeshCore \*ClientACL\* system is separate and applies to nodes like repeaters and sensors (to restrict who can send them commands or telemetry requests). It doesn’t apply to basic messaging. There’s no concept like “read-only member” or “admin member” within a channel. Every node with the key can send and receive freely. Moderation must be social (or by kicking someone out via key change). \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9-security-and-access-control)



\*\*Profile and Frequency Settings:\*\* It’s worth noting that channel management as discussed here assumes all nodes are on the same radio frequency profile (e.g., using the same band, spreading factor, etc., which is part of NodePrefs). If not, they physically can’t hear each other. The question of “channel configuration uses radio channels to separate networks” from LocalMesh website might refer to ensuring separate groups use different frequencies to not interfere. But within a given frequency plan, our “channels” are encryption groups. So the SDK should ensure the devices also share radio settings (the SDK might provide a way to set region parameters in NodePrefs, but that’s aside from logical channels).



\*\*Edge Cases in Channel Management:\*\*



\*   If you try to set a channel with a name longer than 31 characters, it will be truncated (32 including null). The system likely handles that quietly.

\*   If you input a Base64 key that isn’t exactly 22 or 43 chars (for 16 or 32 bytes respectively), the add might fail or interpret incorrectly. Ideally, the interface would validate the length.

\*   If two channels inadvertently ended up with the same secret (rare unless user deliberately sets it so or uses a short key), their channel hashes would collide, and the device might get confused because it wouldn’t know which “name” to attribute incoming messages to. The safe design is to prevent adding a channel with a key equal to an existing one (or at least warn). But I’m not sure if MeshCore explicitly prevents duplicate keys in the list. It might just allow it (since name difference differentiates them, but then incoming messages would match both unless code checks and avoids duplicates). This is a minor corner case; likely not encountered since random keys won’t collide.



\*\*Home Assistant Integration:\*\* The FAQ references a \*MeshCore for Home Assistant\* project. This likely allows Home Assistant to interface with a MeshCore network (perhaps via a companion radio or MQTT). For example, you might have sensors broadcasting on a channel, and Home Assistant listens and turns them into events. The integration would need to configure the channel key in the Home Assistant system so it can decode messages. Possibly the integration simply bridges via the meshcore-cli or pyMC library, meaning it uses the same channel management commands under the hood. So, an admin would input the channel keys into the Home Assistant integration’s config (much like adding a Meshtastic channel). The integration might then join that channel to receive data or to allow HA to send commands (like turning on lights via LoRa). \[\\\[github.com\\]](https://github.com/LitBomb/MeshCore-FAQ)



For the .NET MeshCore SDK, one can imagine providing functions like:



```csharp

meshCoreDevice.AddChannel(string name, string base64Key);

meshCoreDevice.RemoveChannel(string name);

Channel myChannel = meshCoreDevice.GetChannel(int index);

IEnumerable<Channel> channels = meshCoreDevice.ListChannels();

```



Where `Channel` is a class with properties Name and Key (likely we wouldn’t expose the actual key in plaintext by default for security, but as developer you might need it to e.g. share with another device, so perhaps it’s available when explicitly requested).



The SDK should ensure the channel file updates are flushed (though the device does that after 5s automatically, it might be worth waiting a moment or calling a save function if available).



\*\*Memory and CPU Impact of Channel Ops:\*\* These operations are extremely fast (just file read/write of 68 bytes, etc.). Only thing is writing triggers flash writes – doing it too often could wear memory (hence the lazy delay). But normal usage of adding or removing channels occasionally is fine.



\*\*Summarizing Commands in a Table:\*\*



| Operation                           | CLI Command (meshcore-cli)                                                  | Description                                                                                                                                            |

| ----------------------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |

| List all channels                   | `get\_channels`                                                              | Displays all stored channels (index, name, key) \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli).          |

| Show specific channel               | `get\_channel <index or name>`                                               | Shows details of one channel.                                                                                                                          |

| Add a new channel                   | `set\_channel <index> "<Name>" <Base64Key>`                                  | Adds or updates channel at given index with name and key \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli). |

| Remove a channel                    | `remove\_channel <index or name>`                                            | Deletes the specified channel from list \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli).                  |

| Send message on channel             | `chan <index> <message>` <br>\*(alias:)\* `ch`                                | Sends a text message to the specified channel \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli).            |

| Send on public channel              | `public <message>` (alias: `dch` for "default channel")                     | Sends text on the Public channel (index 0) \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli).               |

| Enter interactive chat on a channel | \*(Not directly shown; presumably via UI or using send commands repeatedly)\* | In T-Deck UI, select channel to chat. In CLI, just use `chan` for each message or potentially a future feature.                                        |



These cover creation, deletion, and usage. There is no explicit “rename channel” command separate from set (you would just call set\\\_channel with new name but same key at the same index).



\*\*Channel Synchronization Across Devices:\*\* There is no automatic sync of channel configs between your devices. If a user has multiple MeshCore devices (say a handheld and a base station), they’d need to configure the channel on each. However, they could use the same QR or link to import channels on each.



\*\*Resetting channels:\*\* If needed, resetting the device’s mesh data (contacts and channels) could be done by wiping those files. That typically only happens if a user wants to clear memory. The CLI might not have a single command for it, but one can remove all one by one or reflash firmware with a “factory reset” flag.



\*\*Example use-case:\*\* Suppose a disaster response team wants to set up a private mesh channel for communications:



1\.  Team leader runs `set\_channel 1 "TeamAlpha" <generatedKey>` on their device (which already has Public at 0). Now channel1 is TeamAlpha.

2\.  They then share that via QR code. The rest of the team, using the MeshCore mobile app, scans the QR. The app internally sends the `set\_channel` command to their paired device (or does it via BLE).

3\.  All devices now have channel TeamAlpha (in addition to the default Public).

4\.  Team decides to operate on TeamAlpha exclusively for security. Each user on their device selects TeamAlpha as active channel or knows to send on it (the companion app might show a list of channels and let them pick TeamAlpha as default).

5\.  During operations, if someone needs to reach a person outside the team, they could switch to Public or another channel if configured.

6\.  After operations, if they want to invite another group to join, they could give them the TeamAlpha key (thus expanding membership). Or if they worry a key leaked, they do `remove\_channel TeamAlpha` (or just ignore it) and `set\_channel 1 "TeamAlpha2" <newKey>` on their devices, distribute the new key.



This shows how channels are manually managed but flexible.



\*\*Interfacing with other Tools:\*\* The snippet shows Python library dependency for CLI (`python meshcore` pkg). The .NET SDK might either: \[\\\[github.com\\]](https://github.com/meshcore-dev/meshcore-cli)



\*   Communicate with a physical device via serial port, issuing these CLI-like commands,

\*   Or implement the MeshCore protocol natively if they embed a radio (less likely; means reimplement encryption etc. But the user is specifically developing .NET to likely talk to a connected mesh device.)



So the SDK could wrap these operations in easier functions. That’s presumably what the question expects – understanding channels deeply to implement SDK features.



\## 8. Best Practices and Operational Guidance for Channels



Using channels effectively requires some planning and adherence to best practices, especially in larger deployments or mission-critical scenarios. Here are key guidelines:



\*   \*\*Use Private Channels for Privacy:\*\* As mentioned, always prefer creating a private channel with a unique secret for any communication that is not intended for everyone. The default public channel is great for open networks or initial contact, but don’t discuss sensitive information there (the key is public). Instead, spin up a private channel and share the key with your group. This ensures outsiders can’t listen in or even know what’s being said. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9.2-message-encryption-and-acl)



\*   \*\*Keep the Channel Key Secret:\*\* Treat channel secrets like passwords. Distribute them only over secure mediums. If using QR codes, ensure no unauthorized person scans it. Avoid sending the key unencrypted over email or public chat. If possible, meet in person or use a pre-established secure channel to share the mesh channel key.



\*   \*\*Plan Channel Organization:\*\* Think about how many channels you need and what each is for. Having too many can complicate coordination (you might miss a message because you weren’t looking at that channel’s feed). A common approach is:

&nbsp;   \*   One common channel for whole group (e.g. “All Hands” announcements).

&nbsp;   \*   Separate channels for sub-teams or functions if needed (e.g. a logistics team channel, a command channel).

&nbsp;   \*   Maybe an emergency broadcast channel that everyone monitors for urgent alerts.

&nbsp;   But avoid needless fragmentation. Each additional channel means a user has to monitor an additional chat. If one channel can serve the purpose without chaos, stick to one.



\*   \*\*Name Channels Clearly:\*\* Use descriptive names (“Team Alpha”, “SearchAndRescue”, “Logistics”) so users know what they’re for. Remember names are stored in each device and not globally synchronized except by the initial sharing. So ensure everyone uses the same naming convention when adding (the QR should handle that). Clear naming prevents confusion if a device has multiple channels.



\*   \*\*Limit Channel Membership:\*\* The security of a private channel is only as strong as the trustworthiness of its members. Keep the circle small if high confidentiality is required. If someone leaves the team or loses their device, \*\*rotate the channel key\*\* (create a new channel and move the team to it). Yes, that’s inconvenient (everyone must update to the new key), but it’s the only way to lock out a former member.



\*   \*\*Monitor Channel Connectivity:\*\* In a mesh, some distant nodes might not always receive floods (if there are not enough repeaters or if they’re out of range). If you have critical announcements, consider repeating them a couple of times or confirming receipt. Using the public channel as a backup for “mayday” signals is also a tactic – because even if someone wasn’t on your private channel, at least a public broadcast might reach any listener who can relay. This is more of a network design issue: ensure coverage (via repeaters or additional hops) so that channel messages reach everyone.



\*   \*\*Leverage Room Servers for Persistence:\*\* If message retention or asynchronous delivery is important (e.g., if team members might join late and need to catch up on prior messages), deploy a \*room server\*. The room server should be set up with the channel key and ideally placed where it can hear all traffic (or be a repeater itself). This way, anyone who comes online later or has patchy connectivity can query the room server for missed messages. It adds a slight centralized element to an otherwise decentralized mesh, but can greatly improve reliability of comms. \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9-security-and-access-control), \[\\\[deepwiki.com\\]](https://deepwiki.com/ripplebiz/MeshCore/9-security-and-access-control)



\*   \*\*Use Repeaters for Range:\*\* Ensure that if your group is spread out, you have dedicated repeater nodes turned on (and ideally on elevated terrain or with good antennas). Repeaters don’t require joining channels (they forward all), but it might help to configure them with your channel as well, especially if you want them not to forward other traffic. Actually, a repeater with no knowledge of your channel will forward everything anyway – which is fine. But if multiple groups share repeaters, you might inadvertently forward each other’s traffic. Since encryption stops you from seeing each other’s content, it’s not a security problem, but it could use bandwidth. If it’s a concern, coordinate channel frequency or perhaps run separate hardware for separate networks.



\*   \*\*Minimize Traffic on Low-Bandwidth Mesh:\*\* Because LoRa mesh has limited capacity, avoid superfluous chatter or very long messages on a busy channel. Keep messages concise. If something lengthy needs to be conveyed (like a large data file), consider splitting it or doing it peer-to-peer if possible. A channel flood of a big data packet could clog the network for a bit (given LoRa’s low bitrate, e.g. SF7 \\~5kbps).



\*   \*\*Be mindful of “All” channel usage:\*\* Some users keep the Public channel active as a fallback or to interoperate with other MeshCore users in vicinity. That’s fine, but remember anything on Public is not private. A good practice: \*change the default device name if using public channel\*, because that name is broadcast in plain. If you don’t want to reveal identity on public, maybe set a generic name or turn off location advertising (the NodePrefs `advert\_loc\_policy` can disable automatically attaching GPS coords in adverts to public).



\*   \*\*Maintain Updated Channel List:\*\* Remove channels from your device that you no longer need or use. This reduces clutter and the chance of confusion. It also is a minor security measure – if somehow an old channel’s key got compromised and you’re not using it anyway, just delete it so your device isn’t listening to potentially malicious traffic on it.



\*   \*\*Test your channel config\*\*: After setting up a new channel and sharing it, do a round of test messages with all intended participants to ensure everyone has it working. It’s easy for one person to mis-type a key if manually entered, etc. If someone isn’t receiving, double-check their channel settings.



\*\*Scenarios – which channel to use?\*\*:



\*   \*\*Initial network bring-up:\*\* Use Public to find all nodes (since everyone hears adverts here initially). Once you’ve identified nodes, quickly move to a private channel.

\*   \*\*Large event with multiple teams:\*\* Perhaps have a private channel per team, plus one joint channel for coordination among team leads. Each device can have both – team members mostly talk on their team channel, leads also monitor the coordination channel.

\*   \*\*Emergency broadcast:\*\* Ensure at least one channel is known by all for urgent announcements (it could just be the Public channel if you’re willing to sacrifice secrecy for the sake of reaching even outsiders who might assist in an emergency).

\*   \*\*Interoperability:\*\* If you want to allow friendly outsiders to communicate with you without giving them your private key, keep a secondary channel for that (or just use Public in a limited way). Alternatively, set up a temporary channel for multi-group communication and share that with the other group for the duration needed.



\## 9. Troubleshooting Common Channel Issues



Despite the relative simplicity of channels, users may encounter issues. Here are common problems and how to address them:



\*   \*\*“I can’t see messages on the channel”\*\* – If a team member isn’t receiving channel messages:

&nbsp;   \*   \*\*Verify the Channel Key:\*\* The first thing is to ensure their channel configuration exactly matches. A single character off in the Base64 key will result in a totally different secret. Compare keys across devices (perhaps by re-scanning the original QR or deliberately printing the key and checking). Removing and re-adding the channel often fixes a typo issue.

&nbsp;   \*   \*\*Check Name vs Key:\*\* The name doesn’t affect receiving, but it’s useful to confirm they added the correct key under that name (some times people might create a channel with correct name but wrong key or vice versa). Everyone in one channel must share the same key, name can differ without affecting functionality (except maybe confusion). If in doubt, re-import the full correct info.

&nbsp;   \*   \*\*Range/Topology Issues:\*\* If the key is confirmed, the next suspect is network range. Perhaps that device is out of range or there’s a dead zone. Try moving closer or adding a repeater. Also confirm the device’s radio is on the same frequency profile (if someone’s device was set to EU868 and others on US915, they won’t physically hear each other – this is a configuration mismatch, not channel key issue). Frequency/SF mismatches are less obvious – ensure everyone uses the same rf settings profile (often done at firmware flash time or via config).

&nbsp;   \*   \*\*Device Not in Channel Mode:\*\* If the user’s device still primarily uses Public (advertising there and maybe not listening properly on private), it could be that they didn’t switch to the private channel. Many devices can listen to all added channels simultaneously, but on some the concept of “current channel” might matter. On T-Deck, as long as it’s added, it listens. But on a companion phone app, possibly one might toggle which channels are active. Ensure the channel is enabled/active. (MeshCore doesn’t have a per-channel enable toggle as far as we know – if it’s in the list it’s active by default).

&nbsp;   \*   \*\*Pending Contact Addition:\*\* If `manual\_add\_contacts` is enabled, the device might be ignoring messages from nodes it hasn't explicitly added. This is an advanced setting used to avoid automatic addition of unknown senders. If that’s on, then when a new channel message comes from an unknown pubkey, the device might drop it. The solution is to manually add those contacts (e.g., by exchanging contact cards or turning off manual mode temporarily).



\*   \*\*“Unknown device showing up on channel”\*\* – If you see messages or adverts on your private channel from an unknown name or ID, that could mean the channel secret leaked to someone who shouldn’t have it. (Or someone typed the same key by sheer coincidence on their network.) In either case, you have an \*\*eavesdropper/impersonator in the channel\*\*. The remedy is to \*\*change the channel secret immediately\*\*. Remove the current channel (or just abandon it) and coordinate a new channel with a new key among your trusted members. There is no way to disinvite a single member except by changing the secret. If it’s a collision scenario (improbable), the chance of it happening again with a new random key is extremely low.



\*   \*\*“Device X still shows in contacts after channel removed”\*\* – Removing a channel doesn’t remove contacts. If you had encountered or messaged someone via that channel, they’d be in your contacts list (with their Ed25519 identity). They’ll stay there until you remove the contact. This isn’t a problem per se, but users might wonder if those contacts can still reach them. They can via direct messages on contact (if they have a route) or via any other shared channel. But if the channel is gone and you don’t share any other channel or direct link, they effectively won’t be communicating. To avoid confusion, you could also remove such contacts if they are not relevant anymore.



\*   \*\*“Our private channels aren’t private – other people appear”\*\* – Ensure no one accidentally shared the key widely. Also confirm everyone has changed the default device name; if someone left their device name as “MeshNode123” and didn’t realize, they might confuse that with an outsider. (One might see a generic name and think “who is that?” but it could just be one of your own team who didn’t personalize their node). So distinguish actual unknowns from known devices with unclear labels.



\*   \*\*Lost Key\*\* – If a team member loses the record of the channel key (e.g., they factory reset their device or got a new device), you can re-invite them by providing the key again. If \*everyone\* loses it (no one remembers the key but devices still have it stored), you can extract it from one device by using `get\_channels` or viewing the QR from it. It’s a good idea to keep a secure backup of important channel keys in case devices are lost – much like keeping a backup of encryption keys for data.



\*   \*\*Channel file corruption\*\* – Rarely, the `/channels2` file might become corrupted (e.g., if power lost exactly during a write). If that happens, some or all channels might not load on boot. The device might indicate error or you’ll notice channels missing. The remedy is to re-add the channels (since you presumably know the keys), or restore from a backup of the file if you had one. The lazy-write mechanism minimizes this risk (only writing at most every few seconds, not constantly).



\*   \*\*Firmware Upgrade and Channel Compatibility\*\* – Generally, newer firmware uses the same `/channels2` format (68 bytes records). If an upgrade ever changed it, the release notes would mention migrating channels. For example, if one version moved to a different encryption algorithm or larger key, it might introduce a `/channels3`. But currently, that’s not the case. If you do upgrade and find channels gone, check if the file is still there. Perhaps the upgrade reset user data. If so, reconfigure the channels or restore from backup. Always document your channel secrets before major changes, or ensure you have another device in the same channel to retrieve the key from. (This is akin to remembering your Wi-Fi password – if the router resets and you forgot the passphrase, it’s troublesome.)



\*   \*\*Mesh Network Congestion\*\* – If too many channels are active with heavy traffic, you might experience delays or lost messages. On LoRa mesh, you might not have obvious “lags”, but you might see that messages sometimes don’t get through on first try due to collisions. If you suspect this:

&nbsp;   \*   Try staggering transmissions (don’t all talk at once).

&nbsp;   \*   Possibly allocate different SF or frequency for different channels (if using multi-radio devices or if each team can operate on a slightly different frequency plan). That’s advanced and requires all devices support multi-band or be retuned, which MeshCore can do via profiles, but typically all in one mesh use one frequency.

&nbsp;   \*   Add more repeaters to cover more area with fewer hops, which reduces the total number of transmissions needed (thus freeing air time).



\*   \*\*Battery issues\*\* – If a device is in a very busy channel, its battery may drain a bit faster (radio receiving constantly). Additionally, if acknowledgments are disabled for group, devices might not sleep as often waiting for them. It’s not usually significant, but if battery life is a concern, monitor usage. Possibly use lower message rates or lower the radio power (if short range) to save battery.



Finally, users often ask: \*\*“Do I need to do anything special to use channels in the .NET SDK?”\*\* – Aside from sending the appropriate commands to sync channel info with the device, the main thing is handling message routing logic: e.g., labeling incoming messages with which channel they belong to (the device might give you an event with channel index or name). The developer should ensure their app shows the context (so you don’t mix up messages from different channels in the UI). Another gotcha: if sending a direct message versus a channel message, use the correct API so that it sets payload type correctly. The SDK should abstract this – e.g., have a SendMessage(toContact) vs BroadcastMessage(channel) method, so the developer doesn’t accidentally send a personal message to the whole channel or vice versa.



\*\*\*



In conclusion, MeshCore’s channel system is a powerful feature for organizing mesh communications. It provides \*\*group messaging with strong encryption\*\*, minimal overhead, and user-defined flexibility. By understanding its architecture (simple static records) and operation (flood routing, no built-in membership list), and using the available CLI/API commands, one can effectively integrate channel management into applications (like the MeshCore.Net.SDK) and ensure robust, secure team communications on the mesh network.



