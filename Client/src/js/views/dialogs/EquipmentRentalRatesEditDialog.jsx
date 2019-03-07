import React from 'react';
import { Grid, Row, Col } from 'react-bootstrap';
import { FormGroup, HelpBlock, ControlLabel } from 'react-bootstrap';

import * as Constant from '../../constants';

import DropdownControl from '../../components/DropdownControl.jsx';
import EditDialog from '../../components/EditDialog.jsx';
import FormInputControl from '../../components/FormInputControl.jsx';
import Form from '../../components/Form.jsx';

import { isBlank } from '../../utils/string';


var EquipmentRentalRatesEditDialog = React.createClass({
  propTypes: {
    rentalAgreement: React.PropTypes.object.isRequired,
    onSave: React.PropTypes.func.isRequired,
    onClose: React.PropTypes.func.isRequired,
    show: React.PropTypes.bool,
  },

  getInitialState() {
    return {
      equipmentRate: this.props.rentalAgreement.equipmentRate || 0,
      ratePeriod: this.props.rentalAgreement.ratePeriod || Constant.RENTAL_RATE_PERIOD_HOURLY,
      rateComment: this.props.rentalAgreement.rateComment || '',

      equipmentRateError: '',
      ratePeriodError: '',
    };
  },

  updateState(state, callback) {
    this.setState(state, callback);
  },

  didChange() {
    if (this.state.equipmentRate !== this.props.rentalAgreement.equipmentRate) { return true; }
    if (this.state.ratePeriod !== this.props.rentalAgreement.ratePeriod) { return true; }
    if (this.state.rateComment !== this.props.rentalAgreement.rateComment) { return true; }

    return false;
  },

  isValid() {
    this.setState({
      equipmentRateError: '',
      ratePeriodError: '',
    });

    var valid = true;

    if (isBlank(this.state.equipmentRate) ) {
      this.setState({ equipmentRateError: 'Pay rate is required' });
      valid = false;
    } else if (this.state.equipmentRate < 1) {
      this.setState({ equipmentRateError: 'Pay rate not valid' });
      valid = false;
    }

    if (isBlank(this.state.ratePeriod)) {
      this.setState({ ratePeriodError: 'Period is required' });
      valid = false;
    }

    return valid;
  },

  onSave() {
    this.props.onSave({ ...this.props.rentalAgreement, ...{
      equipmentRate: this.state.equipmentRate,
      ratePeriod: this.state.ratePeriod,
      rateComment: this.state.rateComment,
    }});
  },

  render() {
    // Read-only if the user cannot edit the rental agreement
    var isReadOnly = !this.props.rentalAgreement.canEdit && this.props.rentalAgreement.id !== 0;
    var ratePeriods = [ Constant.RENTAL_RATE_PERIOD_HOURLY, Constant.RENTAL_RATE_PERIOD_DAILY, Constant.RENTAL_RATE_PERIOD_WEEKLY, Constant.RENTAL_RATE_PERIOD_MONTHLY, Constant.RENTAL_RATE_PERIOD_NEGOTIATED ];

    return <EditDialog id="rental-agreements-edit" show={ this.props.show }
      onClose={ this.props.onClose } onSave={ this.onSave } didChange={ this.didChange } isValid={ this.isValid }
      title={<strong>Rental Agreement</strong>}>
      <Form>
        <Grid fluid>
          <Row>
            <Col md={4}>
              <FormGroup controlId="equipmentRate" validationState={ this.state.equipmentRateError ? 'error' : null }>
                <ControlLabel>Pay Rate <sup>*</sup></ControlLabel>
                <FormInputControl type="float" min={ 0 } defaultValue={ this.state.equipmentRate.toFixed(2) } readOnly={ isReadOnly } updateState={ this.updateState } autoFocus/>
                <HelpBlock>{ this.state.equipmentRateError }</HelpBlock>
              </FormGroup>
            </Col>
            <Col md={2}>
              <FormGroup controlId="ratePeriod" validationState={ this.state.ratePeriodError ? 'error' : null }>
                <ControlLabel>Period <sup>*</sup></ControlLabel>
                <DropdownControl id="ratePeriod" disabled={ isReadOnly } title={ this.state.ratePeriod } updateState={ this.updateState }
                  items={ ratePeriods } />
                <HelpBlock>{ this.state.ratePeriodError }</HelpBlock>
              </FormGroup>
            </Col>
            <Col md={6}>
              <FormGroup controlId="rateComment">
                <ControlLabel>Comment</ControlLabel>
                <FormInputControl defaultValue={ this.state.rateComment } readOnly={ isReadOnly } updateState={ this.updateState } />
              </FormGroup>
            </Col>
          </Row>
        </Grid>
      </Form>
    </EditDialog>;
  },
});

export default EquipmentRentalRatesEditDialog;
