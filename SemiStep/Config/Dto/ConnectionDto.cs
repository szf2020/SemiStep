using YamlDotNet.Serialization;

namespace Config.Dto;

public sealed class ConnectionDto
{
	[YamlMember(Alias = "connection_file_version")]
	public string? ConnectionFileVersion { get; set; }

	[YamlMember(Alias = "ip")]
	public string? Ip { get; set; }

	[YamlMember(Alias = "connection_protocol")]
	public string? ConnectionProtocol { get; set; }

	[YamlMember(Alias = "plc_rack")]
	public int? PlcRack { get; set; }

	[YamlMember(Alias = "plc_slot")]
	public int? PlcSlot { get; set; }

	[YamlMember(Alias = "max_retries_attempts")]
	public int? MaxRetriesAttempts { get; set; }

	[YamlMember(Alias = "polling_interval_ms")]
	public int? PollingIntervalMs { get; set; }

	[YamlMember(Alias = "writing_timeout_ms")]
	public int? WritingTimeoutMs { get; set; }

	[YamlMember(Alias = "commit_timeout_ms")]
	public int? CommitTimeoutMs { get; set; }

	[YamlMember(Alias = "header_db_number")]
	public int? HeaderDbNumber { get; set; }

	[YamlMember(Alias = "magic_number_offset")]
	public int? MagicNumberOffset { get; set; }

	[YamlMember(Alias = "word_order_offset")]
	public int? WordOrderOffset { get; set; }

	[YamlMember(Alias = "protocol_version_offset")]
	public int? ProtocolVersionOffset { get; set; }

	[YamlMember(Alias = "managing_db_number_offset")]
	public int? ManagingDbNumberOffset { get; set; }

	[YamlMember(Alias = "int_db_number_offset")]
	public int? IntDbNumberOffset { get; set; }

	[YamlMember(Alias = "float_db_number_offset")]
	public int? FloatDbNumberOffset { get; set; }

	[YamlMember(Alias = "string_db_number_offset")]
	public int? StringDbNumberOffset { get; set; }

	[YamlMember(Alias = "execution_db_number_offset")]
	public int? ExecutionDbNumberOffset { get; set; }

	[YamlMember(Alias = "header_db_total_size")]
	public int? HeaderDbTotalSize { get; set; }

	[YamlMember(Alias = "managing_db_number")]
	public int? ManagingDbNumber { get; set; }

	[YamlMember(Alias = "pc_status_offset")]
	public int? PcStatusOffset { get; set; }

	[YamlMember(Alias = "pc_transaction_id_offset")]
	public int? PcTransactionIdOffset { get; set; }

	[YamlMember(Alias = "pc_checksum_int_offset")]
	public int? PcChecksumIntOffset { get; set; }

	[YamlMember(Alias = "pc_checksum_float_offset")]
	public int? PcChecksumFloatOffset { get; set; }

	[YamlMember(Alias = "pc_checksum_string_offset")]
	public int? PcChecksumStringOffset { get; set; }

	[YamlMember(Alias = "pc_recipe_lines_offset")]
	public int? PcRecipeLinesOffset { get; set; }

	[YamlMember(Alias = "plc_status_offset")]
	public int? PlcStatusOffset { get; set; }

	[YamlMember(Alias = "plc_error_offset")]
	public int? PlcErrorOffset { get; set; }

	[YamlMember(Alias = "plc_stored_id_offset")]
	public int? PlcStoredIdOffset { get; set; }

	[YamlMember(Alias = "plc_checksum_int_offset")]
	public int? PlcChecksumIntOffset { get; set; }

	[YamlMember(Alias = "plc_checksum_float_offset")]
	public int? PlcChecksumFloatOffset { get; set; }

	[YamlMember(Alias = "plc_checksum_string_offset")]
	public int? PlcChecksumStringOffset { get; set; }

	[YamlMember(Alias = "managing_db_total_size")]
	public int? ManagingDbTotalSize { get; set; }

	[YamlMember(Alias = "managing_pc_data_size")]
	public int? ManagingPcDataSize { get; set; }

	[YamlMember(Alias = "int_db_number")]
	public int? IntDbNumber { get; set; }

	[YamlMember(Alias = "int_db_total_capacity_offset")]
	public int? IntDbTotalCapacityOffset { get; set; }

	[YamlMember(Alias = "int_db_current_size_offset")]
	public int? IntDbCurrentSizeOffset { get; set; }

	[YamlMember(Alias = "int_db_data_offset")]
	public int? IntDbDataOffset { get; set; }

	[YamlMember(Alias = "float_db_number")]
	public int? FloatDbNumber { get; set; }

	[YamlMember(Alias = "float_db_total_capacity_offset")]
	public int? FloatDbTotalCapacityOffset { get; set; }

	[YamlMember(Alias = "float_db_current_size_offset")]
	public int? FloatDbCurrentSizeOffset { get; set; }

	[YamlMember(Alias = "float_db_data_offset")]
	public int? FloatDbDataOffset { get; set; }

	[YamlMember(Alias = "string_db_number")]
	public int? StringDbNumber { get; set; }

	[YamlMember(Alias = "string_db_total_capacity_offset")]
	public int? StringDbTotalCapacityOffset { get; set; }

	[YamlMember(Alias = "string_db_current_size_offset")]
	public int? StringDbCurrentSizeOffset { get; set; }

	[YamlMember(Alias = "string_db_data_offset")]
	public int? StringDbDataOffset { get; set; }

	[YamlMember(Alias = "execution_db_number")]
	public int? ExecutionDbNumber { get; set; }

	[YamlMember(Alias = "recipe_active_offset")]
	public int? RecipeActiveOffset { get; set; }

	[YamlMember(Alias = "actual_line_offset")]
	public int? ActualLineOffset { get; set; }

	[YamlMember(Alias = "step_current_time_offset")]
	public int? StepCurrentTimeOffset { get; set; }

	[YamlMember(Alias = "for_loop_count1_offset")]
	public int? ForLoopCount1Offset { get; set; }

	[YamlMember(Alias = "for_loop_count2_offset")]
	public int? ForLoopCount2Offset { get; set; }

	[YamlMember(Alias = "for_loop_count3_offset")]
	public int? ForLoopCount3Offset { get; set; }

	[YamlMember(Alias = "execution_db_total_size")]
	public int? ExecutionDbTotalSize { get; set; }
}
